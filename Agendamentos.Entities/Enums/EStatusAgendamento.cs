// ============================================================================
// Arquivo: Enums/StatusAgendamento.cs
// Descrição: Enum que define os possíveis estados de um agendamento.
//            Representa o ciclo de vida de um agendamento no sistema.
// ============================================================================

namespace Agendamentos.Entities.Enums;

/// <summary>
/// Representa os possíveis estados de um agendamento.
/// O fluxo típico é: Pendente → Confirmado → Concluido
/// O agendamento pode ser cancelado a qualquer momento antes de ser concluído.
/// </summary>
public enum EStatusAgendamento
{
    /// <summary>
    /// Agendamento recém-criado, aguardando confirmação do prestador.
    /// </summary>
    Pendente = 0,

    /// <summary>
    /// Agendamento confirmado pelo prestador de serviços.
    /// </summary>
    Confirmado = 1,

    /// <summary>
    /// Agendamento cancelado pelo cliente ou pelo prestador.
    /// </summary>
    Cancelado = 2,

    /// <summary>
    /// Serviço realizado com sucesso. Estado final do fluxo normal.
    /// </summary>
    Concluido = 3
}