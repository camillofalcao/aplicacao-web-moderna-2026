// ============================================================================
// Testes unitários para a entidade HorarioDisponivel (Rich Domain Model).
//
// CONCEITO IMPORTANTE (Testes de Limites — Boundary Testing):
// O método Contem() usa uma comparação "meio aberta": hora >= HoraInicio
// e hora < HoraFim. Os testes verificam esses limites explicitamente:
// - HoraInicio é INCLUÍDA (>=)
// - HoraFim é EXCLUÍDA (<)
// Isso é uma decisão de negócio: se o expediente é 08:00-17:00,
// um agendamento às 08:00 é aceito, mas às 17:00 não.
// ============================================================================

using Agendamentos.Entities.Models;
using FluentAssertions;

namespace Agendamentos.Tests.Models;

/// <summary>
/// Testes para o método Contem() da entidade HorarioDisponivel.
/// </summary>
public class HorarioDisponivelTests
{
    /// <summary>
    /// Horário de referência: Segunda-feira, 08:00 às 17:00.
    /// </summary>
    private static HorarioDisponivel CriarHorarioPadrao() => new()
    {
        PrestadorId = 1,
        DiaSemana = DayOfWeek.Monday,
        HoraInicio = new TimeOnly(8, 0),
        HoraFim = new TimeOnly(17, 0)
    };

    [Fact]
    public void Contem_HorarioDentroDoExpediente_DeveRetornarTrue()
    {
        var horario = CriarHorarioPadrao();

        horario.Contem(DayOfWeek.Monday, new TimeOnly(10, 30))
            .Should().BeTrue();
    }

    [Fact]
    public void Contem_ExatamenteNoInicio_DeveRetornarTrue()
    {
        // Boundary: HoraInicio é inclusiva (>=)
        var horario = CriarHorarioPadrao();

        horario.Contem(DayOfWeek.Monday, new TimeOnly(8, 0))
            .Should().BeTrue();
    }

    [Fact]
    public void Contem_ExatamenteNoFim_DeveRetornarFalse()
    {
        // Boundary: HoraFim é exclusiva (<)
        var horario = CriarHorarioPadrao();

        horario.Contem(DayOfWeek.Monday, new TimeOnly(17, 0))
            .Should().BeFalse();
    }

    [Fact]
    public void Contem_AntesDoInicio_DeveRetornarFalse()
    {
        var horario = CriarHorarioPadrao();

        horario.Contem(DayOfWeek.Monday, new TimeOnly(7, 59))
            .Should().BeFalse();
    }

    [Fact]
    public void Contem_DiaDiferente_DeveRetornarFalse()
    {
        // Horário correto, mas dia errado
        var horario = CriarHorarioPadrao();

        horario.Contem(DayOfWeek.Tuesday, new TimeOnly(10, 0))
            .Should().BeFalse();
    }

    [Fact]
    public void Contem_UmMinutoAntesDoFim_DeveRetornarTrue()
    {
        var horario = CriarHorarioPadrao();

        horario.Contem(DayOfWeek.Monday, new TimeOnly(16, 59))
            .Should().BeTrue();
    }
}
