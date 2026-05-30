// ============================================================================
// Testes unitários para CriarUsuarioValidator (FluentValidation).
//
// CONCEITO IMPORTANTE (Testes de Validação):
// Os validators do FluentValidation são classes que definem regras de
// validação para entidades. Os testes verificam que:
// 1. Dados VÁLIDOS passam na validação (sem erros)
// 2. Cada campo INVÁLIDO gera a mensagem de erro correta
//
// O FluentValidation fornece um TestHelper, mas usamos a abordagem
// direta (chamar ValidateAsync) por ser mais explícita e didática.
// ============================================================================

using Agendamentos.Entities.Models;
using Agendamentos.Entities.UseCases.Usuarios;
using FluentAssertions;

namespace Agendamentos.Tests.Validators;

public class CriarUsuarioValidatorTests
{
    private readonly CriarUsuarioValidator _validator = new();

    [Fact]
    public async Task Validar_DadosValidos_DevePassarSemErros()
    {
        // Arrange
        var usuario = new Usuario { Nome = "João Silva", Email = "joao@email.com" };

        // Act
        var resultado = await _validator.ValidateAsync(usuario);

        // Assert
        resultado.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Validar_NomeVazio_DeveRetornarErro(string? nome)
    {
        var usuario = new Usuario { Nome = nome!, Email = "joao@email.com" };

        var resultado = await _validator.ValidateAsync(usuario);

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == "Nome");
    }

    [Fact]
    public async Task Validar_NomeMenorQue3Caracteres_DeveRetornarErro()
    {
        var usuario = new Usuario { Nome = "Ab", Email = "joao@email.com" };

        var resultado = await _validator.ValidateAsync(usuario);

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e =>
            e.PropertyName == "Nome" &&
            e.ErrorMessage.Contains("3 caracteres"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Validar_EmailVazio_DeveRetornarErro(string? email)
    {
        var usuario = new Usuario { Nome = "João Silva", Email = email! };

        var resultado = await _validator.ValidateAsync(usuario);

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Theory]
    [InlineData("email-sem-arroba")]
    [InlineData("email@")]
    [InlineData("@dominio.com")]
    public async Task Validar_EmailInvalido_DeveRetornarErro(string email)
    {
        var usuario = new Usuario { Nome = "João Silva", Email = email };

        var resultado = await _validator.ValidateAsync(usuario);

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e =>
            e.PropertyName == "Email" &&
            e.ErrorMessage.Contains("válido"));
    }
}
