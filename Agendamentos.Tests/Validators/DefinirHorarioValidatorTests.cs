// ============================================================================
// Testes unitários para DefinirHorarioValidator (FluentValidation).
// ============================================================================

using Agendamentos.Entities.Models;
using Agendamentos.Entities.UseCases.Horarios;
using FluentAssertions;

namespace Agendamentos.Tests.Validators;

public class DefinirHorarioValidatorTests
{
    private readonly DefinirHorarioValidator _validator = new();

    [Fact]
    public async Task Validar_DadosValidos_DevePassarSemErros()
    {
        var horario = new HorarioDisponivel
        {
            PrestadorId = 1,
            DiaSemana = DayOfWeek.Monday,
            HoraInicio = new TimeOnly(8, 0),
            HoraFim = new TimeOnly(17, 0)
        };

        var resultado = await _validator.ValidateAsync(horario);

        resultado.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validar_PrestadorIdZero_DeveRetornarErro()
    {
        var horario = new HorarioDisponivel
        {
            PrestadorId = 0,
            HoraInicio = new TimeOnly(8, 0),
            HoraFim = new TimeOnly(17, 0)
        };

        var resultado = await _validator.ValidateAsync(horario);

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == "PrestadorId");
    }

    [Fact]
    public async Task Validar_HoraFimAnteriorAHoraInicio_DeveRetornarErro()
    {
        var horario = new HorarioDisponivel
        {
            PrestadorId = 1,
            DiaSemana = DayOfWeek.Monday,
            HoraInicio = new TimeOnly(17, 0),
            HoraFim = new TimeOnly(8, 0) // FIM antes do INÍCIO
        };

        var resultado = await _validator.ValidateAsync(horario);

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e =>
            e.PropertyName == "HoraFim" &&
            e.ErrorMessage.Contains("posterior"));
    }

    [Fact]
    public async Task Validar_HoraFimIgualAHoraInicio_DeveRetornarErro()
    {
        var horario = new HorarioDisponivel
        {
            PrestadorId = 1,
            DiaSemana = DayOfWeek.Monday,
            HoraInicio = new TimeOnly(10, 0),
            HoraFim = new TimeOnly(10, 0) // IGUAIS
        };

        var resultado = await _validator.ValidateAsync(horario);

        resultado.IsValid.Should().BeFalse();
    }
}
