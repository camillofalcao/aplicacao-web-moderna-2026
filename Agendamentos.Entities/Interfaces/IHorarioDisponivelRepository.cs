// ============================================================================
// Arquivo: Interfaces/IHorarioDisponivelRepository.cs
// Descrição: Interface que define o contrato do repositório de Horários.
// ============================================================================

using Agendamentos.Entities.Models;

namespace Agendamentos.Entities.Interfaces;

/// <summary>
/// Contrato para operações de acesso a dados de Horários Disponíveis.
/// </summary>
public interface IHorarioDisponivelRepository
{
    /// <summary>
    /// Lista todos os horários disponíveis de um prestador.
    /// </summary>
    Task<List<HorarioDisponivel>> ListarPorPrestadorIdAsync(long prestadorId);

    /// <summary>
    /// Adiciona um novo horário disponível.
    /// </summary>
    Task AdicionarAsync(HorarioDisponivel horario);

    /// <summary>
    /// Remove um horário disponível.
    /// </summary>
    Task RemoverAsync(HorarioDisponivel horario);

    /// <summary>
    /// Busca um horário específico pelo ID.
    /// </summary>
    Task<HorarioDisponivel?> ObterPorIdAsync(long id);

    /// <summary>
    /// Remove todos os horários disponíveis de um prestador.
    /// Utilizado na substituição completa da agenda semanal —
    /// remove os existentes antes de inserir os novos.
    /// </summary>
    Task RemoverTodosPorPrestadorIdAsync(long prestadorId);
}
