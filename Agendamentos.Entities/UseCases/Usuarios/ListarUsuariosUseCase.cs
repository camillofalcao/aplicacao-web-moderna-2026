// ============================================================================
// Arquivo: UseCases/Usuarios/ListarUsuariosUseCase.cs
// Descrição: Caso de uso para listagem de todos os usuários do sistema.
//
// CONCEITO IMPORTANTE (Caso de Uso Simples):
// Nem todo caso de uso precisa de lógica complexa. Este apenas busca dados
// e os retorna. Em Clean Architecture, mesmo operações simples passam pela
// camada de casos de uso para manter a consistência do fluxo.
//
// CONCEITO IMPORTANTE (Retornando Entidades):
// O caso de uso retorna a lista de entidades Usuario diretamente.
// O mapeamento para DTO (omitindo dados sensíveis como SenhaHash)
// é responsabilidade do endpoint na camada API.
// ============================================================================

using Agendamentos.Entities.Common;
using Agendamentos.Entities.Interfaces;
using Agendamentos.Entities.Models;
using ErrorOr;

namespace Agendamentos.Entities.UseCases.Usuarios;

/// <summary>
/// Caso de uso responsável por listar usuários do sistema com paginação.
/// Acessa o repositório de usuários e retorna as entidades paginadas.
/// O endpoint mapeia as entidades para DTOs antes de responder.
/// Restrito a administradores (a autorização é feita no endpoint).
/// </summary>
public class ListarUsuariosUseCase
{
    private readonly IUsuarioRepository _usuarioRepository;

    public ListarUsuariosUseCase(IUsuarioRepository usuarioRepository)
    {
        _usuarioRepository = usuarioRepository;
    }

    /// <summary>
    /// Executa a listagem de usuários com paginação por cursor.
    ///
    /// Fluxo:
    /// 1. Busca limite+1 usuários do repositório (a partir do cursor)
    /// 2. Constrói a ListaPaginada (detecta se há mais itens pelo +1 extra)
    /// 3. Retorna o resultado paginado encapsulado em ErrorOr
    /// </summary>
    /// <param name="cursor">ID do último item da página anterior (null para primeira página).</param>
    /// <param name="limite">Quantidade de itens por página.</param>
    public async Task<ErrorOr<ListaPaginada<Usuario>>> ExecutarAsync(
        int? cursor, int limite)
    {
        var usuarios = await _usuarioRepository.ObterTodosAsync(cursor, limite + 1);
        return ListaPaginada<Usuario>.Criar(usuarios, limite, u => u.Id);
    }
}
