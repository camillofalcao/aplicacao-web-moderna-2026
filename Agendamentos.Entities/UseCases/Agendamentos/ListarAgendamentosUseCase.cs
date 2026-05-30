// ============================================================================
// Arquivo: UseCases/Agendamentos/ListarAgendamentosUseCase.cs
// Descrição: Caso de uso para listagem de agendamentos do usuário logado.
//
// CONCEITO IMPORTANTE (Perspectiva Cliente vs Prestador):
// Este caso de uso implementa uma lógica de "dual perspective":
// - Se o usuário é CLIENTE → busca agendamentos onde ele é o ClienteId
// - Se o usuário é PRESTADOR → primeiro descobre o PrestadorId pelo UsuarioId,
//   depois busca agendamentos onde ele é o PrestadorId
//
// Essa abordagem evita duplicação de código — um único caso de uso atende
// ambos os perfis de usuário com filtros diferentes.
// ============================================================================

using Agendamentos.Entities.Common;
using Agendamentos.Entities.Interfaces;
using Agendamentos.Entities.Models;
using Agendamentos.Entities.UseCases.Common;
using ErrorOr;

namespace Agendamentos.Entities.UseCases.Agendamentos;

/// <summary>
/// Caso de uso responsável por listar agendamentos do usuário logado
/// com paginação por cursor (keyset pagination).
///
/// Retorna uma lista paginada de agendamentos do ponto de vista do usuário:
/// - Clientes veem seus próprios agendamentos
/// - Prestadores veem os agendamentos marcados com eles
/// </summary>
public class ListarAgendamentosUseCase
{
    private readonly IAgendamentoRepository _agendamentoRepo;
    private readonly IPrestadorRepository _prestadorRepo;

    public ListarAgendamentosUseCase(
        IAgendamentoRepository agendamentoRepo,
        IPrestadorRepository prestadorRepo)
    {
        _agendamentoRepo = agendamentoRepo;
        _prestadorRepo = prestadorRepo;
    }

    /// <summary>
    /// Executa a listagem paginada de agendamentos para o usuário informado.
    /// </summary>
    /// <param name="usuarioId">ID do usuário logado (extraído do JWT).</param>
    /// <param name="ehPrestador">Se true, busca pela perspectiva do prestador.</param>
    /// <param name="cursor">ID do último item da página anterior (null para primeira página).</param>
    /// <param name="limite">Quantidade de itens por página.</param>
    /// <param name="ct">Token de cancelamento.</param>
    public async Task<ErrorOr<ListaPaginada<Agendamento>>> ExecutarAsync(
        int usuarioId,
        bool ehPrestador,
        int? cursor,
        int limite,
        CancellationToken ct)
    {
        List<Agendamento> agendamentos;

        if (ehPrestador)
        {
            // CAMINHO DO PRESTADOR: descobre PrestadorId a partir do UsuarioId
            var prestador = await _prestadorRepo.ObterPorUsuarioIdAsync(usuarioId);

            if (prestador is null)
                return Erros.Prestador.NaoEncontrado;

            agendamentos = await _agendamentoRepo
                .ListarPorPrestadorIdAsync(prestador.Id, cursor, limite + 1);
        }
        else
        {
            // CAMINHO DO CLIENTE: filtro direto pelo UsuarioId
            agendamentos = await _agendamentoRepo
                .ListarPorClienteIdAsync(usuarioId, cursor, limite + 1);
        }

        return ListaPaginada<Agendamento>.Criar(agendamentos, limite, a => a.Id);
    }
}
