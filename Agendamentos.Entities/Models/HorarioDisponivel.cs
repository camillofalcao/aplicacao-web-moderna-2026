// ============================================================================
// Arquivo: Models/HorarioDisponivel.cs
// Descrição: Entidade que define os horários disponíveis de um prestador.
//            Cada registro indica um dia da semana com hora de início e fim.
//            O sistema usa esses dados para calcular os slots de agendamento.
// ============================================================================

namespace Agendamentos.Entities.Models;

/// <summary>
/// Entidade de domínio que representa um horário disponível do prestador.
///
/// Exemplo: Um dentista que atende segunda a sexta das 08:00 às 17:00
/// teria 5 registros de HorarioDisponivel, um para cada dia da semana.
///
/// O sistema usa DayOfWeek (enum nativo do .NET) para representar os dias,
/// e TimeOnly para representar horários (sem componente de data).
/// </summary>
public class HorarioDisponivel
{
    /// <summary>
    /// Identificador único do horário disponível.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Chave estrangeira para o Prestador dono deste horário.
    /// </summary>
    public long PrestadorId { get; set; }

    /// <summary>
    /// Dia da semana em que o prestador está disponível.
    /// Usa o enum DayOfWeek do .NET: Sunday=0, Monday=1, ..., Saturday=6.
    /// </summary>
    public DayOfWeek DiaSemana { get; set; }

    /// <summary>
    /// Hora de início do expediente neste dia da semana.
    /// Exemplo: 08:00
    /// </summary>
    public TimeOnly HoraInicio { get; set; }

    /// <summary>
    /// Hora de fim do expediente neste dia da semana.
    /// Exemplo: 17:00
    /// </summary>
    public TimeOnly HoraFim { get; set; }

    // ---- Comportamento (Rich Domain Model) ----

    /// <summary>
    /// Verifica se este horário cobre o dia da semana e a hora informados.
    /// Usado para validar se um agendamento está dentro do expediente.
    ///
    /// Exemplo: Se HoraInicio=08:00 e HoraFim=17:00, DiaSemana=Monday:
    ///   Contem(Monday, 10:00) → true  (dentro do expediente)
    ///   Contem(Monday, 17:00) → false (limite superior exclusivo)
    ///   Contem(Tuesday, 10:00) → false (dia diferente)
    /// </summary>
    public bool Contem(DayOfWeek dia, TimeOnly hora)
        => DiaSemana == dia && hora >= HoraInicio && hora < HoraFim;

    // ---- Propriedade de Navegação ----

    /// <summary>
    /// Prestador ao qual este horário pertence.
    /// </summary>
    public Prestador Prestador { get; set; } = null!;
}
