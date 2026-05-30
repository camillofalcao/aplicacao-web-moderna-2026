// ============================================================================
// Testes unitários para CriarAgendamentoValidator (FluentValidation).
// ============================================================================

using Agendamentos.Entities.Models;
using Agendamentos.Entities.UseCases.Agendamentos;
using FluentAssertions;

namespace Agendamentos.Tests.Validators;

public class CriarAgendamentoValidatorTests
{
    private readonly CriarAgendamentoValidator _validator = new();

    [Fact]
    public async Task Validar_DadosValidos_DevePassarSemErros()
    {
        var agendamento = new Agendamento
        {
            PrestadorId = 1,
            ClienteId = 10,
            DataHora = DateTime.UtcNow.AddDays(1) // Futuro
        };

        var resultado = await _validator.ValidateAsync(agendamento);

        resultado.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validar_PrestadorIdZero_DeveRetornarErro()
    {
        var agendamento = new Agendamento
        {
            PrestadorId = 0,
            ClienteId = 10,
            DataHora = DateTime.UtcNow.AddDays(1)
        };

        var resultado = await _validator.ValidateAsync(agendamento);

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == "PrestadorId");
    }

    [Fact]
    public async Task Validar_ClienteIdZero_DeveRetornarErro()
    {
        var agendamento = new Agendamento
        {
            PrestadorId = 1,
            ClienteId = 0,
            DataHora = DateTime.UtcNow.AddDays(1)
        };

        var resultado = await _validator.ValidateAsync(agendamento);

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == "ClienteId");
    }

    [Fact]
    public async Task Validar_DataNoPassado_DeveRetornarErro()
    {
        var agendamento = new Agendamento
        {
            PrestadorId = 1,
            ClienteId = 10,
            DataHora = DateTime.UtcNow.AddHours(-1) // Passado
        };

        var resultado = await _validator.ValidateAsync(agendamento);

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e =>
            e.PropertyName == "DataHora" &&
            e.ErrorMessage.Contains("futuro"));
    }
}
