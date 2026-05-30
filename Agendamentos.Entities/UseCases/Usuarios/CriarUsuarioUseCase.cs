// ============================================================================
// Arquivo: UseCases/Usuarios/CriarUsuarioUseCase.cs
// Descrição: Caso de uso para criação de um novo usuário no sistema.
//
// CONCEITO IMPORTANTE (Use Case Pattern):
// Um caso de uso é uma classe instanciável que encapsula uma operação
// de negócio completa. Diferente do padrão Handler estático, o caso de uso:
// 1. É instanciável — pode ser registrado no container de DI
// 2. Recebe dependências via CONSTRUTOR — o ASP.NET Core resolve automaticamente
// 3. Recebe a ENTIDADE como parâmetro do método ExecutarAsync
// 4. Retorna a ENTIDADE de domínio — o mapeamento para DTO é feito no endpoint
//
// CONCEITO IMPORTANTE (Rich Domain Model):
// A entidade Usuario possui o método DefinirSenha() que encapsula a lógica
// de hashing BCrypt. O caso de uso usa esse método em vez de chamar
// BCrypt diretamente, respeitando o encapsulamento do domínio.
// ============================================================================

using Agendamentos.Entities.Enums;
using Agendamentos.Entities.Interfaces;
using Agendamentos.Entities.Models;
using Agendamentos.Entities.UseCases.Common;
using ErrorOr;

namespace Agendamentos.Entities.UseCases.Usuarios;

/// <summary>
/// Caso de uso responsável por criar um novo usuário (auto-cadastro de clientes).
///
/// Fluxo de execução:
/// 1. Verifica se já existe um usuário com o mesmo e-mail (regra de negócio)
/// 2. Usa o método DefinirSenha() da entidade para gerar o hash BCrypt
/// 3. Configura a entidade Usuario com ERole.Cliente (auto-cadastro)
/// 4. Persiste no banco de dados via repositório + Unit of Work
/// 5. Retorna a entidade Usuario criada (o endpoint mapeia para DTO)
/// </summary>
public class CriarUsuarioUseCase
{
    // ---------------------------------------------------------------
    // Dependências injetadas via construtor
    // ---------------------------------------------------------------
    // Campos readonly garantem que as dependências não sejam alteradas
    // após a construção do objeto (imutabilidade).
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Construtor com injeção de dependência.
    ///
    /// O container de DI do ASP.NET Core resolve automaticamente
    /// essas dependências quando o caso de uso é instanciado.
    /// Como o caso de uso é registrado como Scoped, ele recebe
    /// as mesmas instâncias de repositório e UoW da requisição atual.
    /// </summary>
    /// <param name="usuarioRepository">Repositório para acesso a dados de usuários.</param>
    /// <param name="unitOfWork">Unit of Work para salvar as alterações no banco.</param>
    public CriarUsuarioUseCase(
        IUsuarioRepository usuarioRepository,
        IUnitOfWork unitOfWork)
    {
        _usuarioRepository = usuarioRepository;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Executa a criação de um novo usuário no sistema.
    ///
    /// O endpoint da API cria a entidade Usuario com Nome e Email,
    /// e passa junto a senha em texto plano. O caso de uso é responsável
    /// por fazer o hash da senha (via entidade) e configurar os valores padrão.
    /// </summary>
    /// <param name="usuario">
    ///     Entidade Usuario com Nome e Email preenchidos pelo endpoint.
    ///     Os demais campos (SenhaHash, Role, Ativo, CriadoEm) são
    ///     configurados por este caso de uso.
    /// </param>
    /// <param name="senha">
    ///     Senha em texto plano informada pelo usuário.
    ///     Será convertida em hash BCrypt pela entidade (DefinirSenha).
    ///     A senha em texto plano NUNCA é persistida no banco de dados.
    /// </param>
    /// <param name="ct">Token de cancelamento da operação.</param>
    /// <returns>
    /// ErrorOr com a entidade Usuario em caso de sucesso,
    /// ou Error em caso de falha (ex: e-mail duplicado).
    /// </returns>
    public async Task<ErrorOr<Usuario>> ExecutarAsync(
        Usuario usuario,
        string senha,
        CancellationToken ct)
    {
        // ---------------------------------------------------------------
        // Passo 1: Verificar se o e-mail já está cadastrado
        // ---------------------------------------------------------------
        // Buscamos no banco um usuário com o mesmo e-mail.
        // Se encontrarmos, retornamos um erro de conflito (HTTP 409).
        // Isso garante a unicidade do e-mail como credencial de login.
        var existente = await _usuarioRepository.ObterPorEmailAsync(usuario.Email);

        if (existente is not null)
            return Erros.Usuario.EmailJaExiste;

        // ---------------------------------------------------------------
        // Passo 2: Gerar o hash da senha usando Rich Domain Model
        // ---------------------------------------------------------------
        // A entidade Usuario encapsula a lógica de hashing BCrypt
        // no método DefinirSenha(). Isso mantém a regra de negócio
        // (nunca armazenar senha em texto plano) dentro da entidade.
        usuario.DefinirSenha(senha);

        // ---------------------------------------------------------------
        // Passo 3: Configurar valores padrão para auto-cadastro
        // ---------------------------------------------------------------
        // Auto-cadastro sempre cria um cliente (ERole.Cliente).
        // O administrador pode promover o cliente a prestador posteriormente.
        usuario.Role = ERole.Cliente;
        usuario.Ativo = true;
        usuario.CriadoEm = DateTime.UtcNow;

        // ---------------------------------------------------------------
        // Passo 4: Persistir no banco de dados
        // ---------------------------------------------------------------
        // AdicionarAsync marca a entidade como "Added" no EF Core.
        // SaveChangesAsync efetiva a inserção no banco (INSERT INTO ...).
        await _usuarioRepository.AdicionarAsync(usuario);
        await _unitOfWork.SaveChangesAsync(ct);

        // ---------------------------------------------------------------
        // Passo 5: Retornar a entidade criada
        // ---------------------------------------------------------------
        // O endpoint da API é responsável por converter a entidade
        // em DTO, selecionando apenas os campos públicos.
        return usuario;
    }
}
