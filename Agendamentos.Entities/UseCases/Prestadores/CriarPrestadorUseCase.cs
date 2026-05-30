// ============================================================================
// Arquivo: UseCases/Prestadores/CriarPrestadorUseCase.cs
// Descrição: Caso de uso para criação de um novo prestador de serviço.
//
// CONCEITO IMPORTANTE (Use Case com Múltiplas Dependências):
// Este caso de uso demonstra um cenário realista onde uma operação
// de negócio precisa de múltiplos repositórios, Unit of Work e cache.
// Todas as dependências são injetadas via construtor pelo container de DI.
//
// CONCEITO IMPORTANTE (ICachePrestadores — Inversão de Dependência):
// Em vez de depender diretamente de IDistributedCache (pacote de infraestrutura),
// o caso de uso depende de ICachePrestadores (interface do domínio).
// A implementação concreta (CachePrestadores com Redis) fica na Infrastructure.
// Isso mantém o domínio limpo e independente de tecnologias externas.
//
// FLUXO DE NEGÓCIO:
// 1. Verificar se o usuário existe
// 2. Verificar se o usuário já não é prestador
// 3. Configurar a entidade Prestador recebida
// 4. Atualizar a Role do usuário para Prestador
// 5. Salvar tudo atomicamente (Unit of Work)
// 6. Invalidar o cache de prestadores
// 7. Retornar a entidade Prestador criada
// ============================================================================

using Agendamentos.Entities.Enums;
using Agendamentos.Entities.Interfaces;
using Agendamentos.Entities.Models;
using Agendamentos.Entities.UseCases.Common;
using ErrorOr;

namespace Agendamentos.Entities.UseCases.Prestadores;

/// <summary>
/// Caso de uso responsável por criar um novo prestador de serviço.
///
/// Recebe a entidade Prestador do endpoint (com os dados do formulário)
/// e executa todas as validações de negócio antes de persistir.
/// </summary>
public class CriarPrestadorUseCase
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IPrestadorRepository _prestadorRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICachePrestadores _cache;

    public CriarPrestadorUseCase(
        IUsuarioRepository usuarioRepository,
        IPrestadorRepository prestadorRepository,
        IUnitOfWork unitOfWork,
        ICachePrestadores cache)
    {
        _usuarioRepository = usuarioRepository;
        _prestadorRepository = prestadorRepository;
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    /// <summary>
    /// Executa a criação de um novo prestador.
    /// </summary>
    /// <param name="prestador">
    ///     Entidade Prestador com os dados preenchidos pelo endpoint:
    ///     UsuarioId, Especialidade, Descricao, FotoUrl, ValorConsulta,
    ///     DuracaoAtendimentoMinutos.
    /// </param>
    /// <param name="ct">Token de cancelamento.</param>
    /// <returns>
    ///     ErrorOr contendo a entidade Prestador em caso de sucesso,
    ///     ou Error em caso de falha de negócio.
    /// </returns>
    public async Task<ErrorOr<Prestador>> ExecutarAsync(
        Prestador prestador,
        CancellationToken ct)
    {
        // ---------------------------------------------------------------
        // PASSO 1: Verificar se o usuário existe no sistema
        // ---------------------------------------------------------------
        var usuario = await _usuarioRepository.ObterPorIdAsync(prestador.UsuarioId);

        if (usuario is null)
            return Erros.Usuario.NaoEncontrado;

        // ---------------------------------------------------------------
        // PASSO 2: Verificar se o usuário já não é um prestador
        // ---------------------------------------------------------------
        // Um usuário só pode ter um registro de prestador.
        var prestadorExistente = await _prestadorRepository.ObterPorUsuarioIdAsync(prestador.UsuarioId);

        if (prestadorExistente is not null)
            return Erros.Prestador.UsuarioJaEhPrestador;

        // ---------------------------------------------------------------
        // PASSO 3: Configurar valores padrão da entidade
        // ---------------------------------------------------------------
        prestador.Ativo = true;

        // Adicionamos ao repositório (ainda não foi salvo no banco).
        await _prestadorRepository.AdicionarAsync(prestador);

        // ---------------------------------------------------------------
        // PASSO 4: Atualizar a Role do usuário para Prestador
        // ---------------------------------------------------------------
        // Quando um usuário se torna prestador, sua Role muda de Cliente
        // para Prestador. Isso afeta suas permissões no sistema.
        usuario.Role = ERole.Prestador;
        await _usuarioRepository.AtualizarAsync(usuario);

        // ---------------------------------------------------------------
        // PASSO 5: Salvar todas as alterações atomicamente
        // ---------------------------------------------------------------
        // O Unit of Work garante que AMBAS as operações (criar prestador
        // e atualizar role) sejam salvas juntas. Se uma falhar, nenhuma
        // é persistida (transação atômica).
        await _unitOfWork.SaveChangesAsync(ct);

        // ---------------------------------------------------------------
        // PASSO 6: Invalidar o cache de prestadores
        // ---------------------------------------------------------------
        // Quando criamos um novo prestador, a lista cacheada fica desatualizada.
        // A interface ICachePrestadores abstrai os detalhes do Redis.
        // Se o cache estiver indisponível, a operação não deve falhar —
        // o cache será atualizado naturalmente quando expirar.
        try
        {
            await _cache.InvalidarAsync(ct);
        }
        catch
        {
            // Cache indisponível — não impede a criação do prestador.
        }

        // ---------------------------------------------------------------
        // PASSO 7: Retornar a entidade Prestador criada
        // ---------------------------------------------------------------
        // Populamos a navegação para o endpoint ter acesso ao nome/email.
        prestador.Usuario = usuario;
        return prestador;
    }
}
