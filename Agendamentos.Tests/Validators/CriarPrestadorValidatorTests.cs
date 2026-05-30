// ============================================================================
// Testes unitários para CriarPrestadorValidator (FluentValidation).
// ============================================================================

using Agendamentos.Entities.Models;
using Agendamentos.Entities.UseCases.Prestadores;
using FluentAssertions;

namespace Agendamentos.Tests.Validators;

public class CriarPrestadorValidatorTests
{
    private readonly CriarPrestadorValidator _validator = new();

    private static Prestador CriarPrestadorValido() => new()
    {
        UsuarioId = 1,
        Especialidade = "Dermatologia",
        Descricao = "Especialista em dermatologia clínica e estética",
        ValorConsulta = 200m,
        DuracaoAtendimentoMinutos = 60
    };

    [Fact]
    public async Task Validar_DadosValidos_DevePassarSemErros()
    {
        var prestador = CriarPrestadorValido();

        var resultado = await _validator.ValidateAsync(prestador);

        resultado.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validar_UsuarioIdZero_DeveRetornarErro()
    {
        var prestador = CriarPrestadorValido();
        prestador.UsuarioId = 0;

        var resultado = await _validator.ValidateAsync(prestador);

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == "UsuarioId");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Ab")]
    public async Task Validar_EspecialidadeInvalida_DeveRetornarErro(string? especialidade)
    {
        var prestador = CriarPrestadorValido();
        prestador.Especialidade = especialidade!;

        var resultado = await _validator.ValidateAsync(prestador);

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == "Especialidade");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Curta")]
    public async Task Validar_DescricaoInvalida_DeveRetornarErro(string? descricao)
    {
        var prestador = CriarPrestadorValido();
        prestador.Descricao = descricao!;

        var resultado = await _validator.ValidateAsync(prestador);

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == "Descricao");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Validar_ValorConsultaInvalido_DeveRetornarErro(decimal valor)
    {
        var prestador = CriarPrestadorValido();
        prestador.ValorConsulta = valor;

        var resultado = await _validator.ValidateAsync(prestador);

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == "ValorConsulta");
    }

    [Theory]
    [InlineData(14)]    // Menos que 15 minutos
    [InlineData(481)]   // Mais que 480 minutos (8h)
    public async Task Validar_DuracaoForaDoLimite_DeveRetornarErro(int duracao)
    {
        var prestador = CriarPrestadorValido();
        prestador.DuracaoAtendimentoMinutos = duracao;

        var resultado = await _validator.ValidateAsync(prestador);

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == "DuracaoAtendimentoMinutos");
    }

    [Theory]
    [InlineData(15)]    // Limite inferior (inclusivo)
    [InlineData(480)]   // Limite superior (inclusivo)
    public async Task Validar_DuracaoNosLimites_DevePassar(int duracao)
    {
        var prestador = CriarPrestadorValido();
        prestador.DuracaoAtendimentoMinutos = duracao;

        var resultado = await _validator.ValidateAsync(prestador);

        resultado.IsValid.Should().BeTrue();
    }
}
