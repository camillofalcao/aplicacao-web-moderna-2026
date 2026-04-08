// ============================================================================
// Arquivo: Interfaces/IAgendamentoRepository.cs
// Descrição: Interface que define o contrato do repositório de Agendamentos.
// ============================================================================

using Agendamentos.Entities.Models;

namespace Agendamentos.Entities.Interfaces;

/// <summary>
/// Contrato para operações de acesso a dados de Agendamentos.
/// </summary>
public interface IAgendamentoRepository
{
    /// <summary>
    /// Busca um agendamento pelo seu identificador, incluindo prestador e cliente.
    /// </summary>
    Task<Agendamento?> ObterPorIdAsync(long id);

    /// <summary>
    /// Lista agendamentos de um cliente com paginação por cursor.
    /// Ordenados por Id decrescente (mais recentes primeiro).
    /// </summary>
    /// <param name="clienteId">ID do cliente.</param>
    /// <param name="cursor">ID do último item da página anterior (null para primeira página).</param>
    /// <param name="quantidade">Quantidade de itens a retornar.</param>
    Task<List<Agendamento>> ListarPorClienteIdAsync(long clienteId, long? cursor, int quantidade);

    /// <summary>
    /// Lista agendamentos de um prestador com paginação por cursor.
    /// Ordenados por Id decrescente (mais recentes primeiro).
    /// </summary>
    /// <param name="prestadorId">ID do prestador.</param>
    /// <param name="cursor">ID do último item da página anterior (null para primeira página).</param>
    /// <param name="quantidade">Quantidade de itens a retornar.</param>
    Task<List<Agendamento>> ListarPorPrestadorIdAsync(long prestadorId, long? cursor, int quantidade);

    /// <summary>
    /// Verifica se já existe um agendamento no mesmo horário para o prestador.
    /// Evita conflitos de agenda (dois clientes no mesmo horário).
    /// </summary>
    Task<bool> ExisteConflito(long prestadorId, DateTime dataHora, long? agendamentoIdExcluir = null);

    /// <summary>
    /// Adiciona um novo agendamento ao banco.
    /// </summary>
    Task AdicionarAsync(Agendamento agendamento);

    /// <summary>
    /// Atualiza os dados de um agendamento existente.
    /// </summary>
    Task AtualizarAsync(Agendamento agendamento);
}
