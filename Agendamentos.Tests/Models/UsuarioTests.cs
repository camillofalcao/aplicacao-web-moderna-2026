using Agendamentos.Entities.Enums;
using Agendamentos.Entities.Models;
using FluentAssertions;

namespace Agendamentos.Tests.Models;

public class UsuarioTests
{
    [Fact]
    public void DefinirSenha_DevePreencherSenhaHash()
    {
        var usuario = new Usuario();
        usuario.DefinirSenha("MinhaSenh@123");
        usuario.SenhaHash.Should().NotBeNullOrWhiteSpace();
        usuario.SenhaHash.Should().StartWith("$2");
    }

    [Fact]
    public void DefinirSenha_NaoDeveArmazenarSenhaEmTextoPlano()
    {
        var senha = "MinhaSenh@123";
        var usuario = new Usuario();
        usuario.DefinirSenha(senha);
        usuario.SenhaHash.Should().NotBe(senha);
    }

    [Fact]
    public void VerificarSenha_SenhaCorreta_DeveRetornarTrue()
    {
        var usuario = new Usuario();
        usuario.DefinirSenha("MinhaSenh@123");
        usuario.VerificarSenha("MinhaSenh@123").Should().BeTrue();
    }

    [Fact]
    public void VerificarSenha_SenhaIncorreta_DeveRetornarFalse()
    {
        var usuario = new Usuario();
        usuario.DefinirSenha("MinhaSenh@123");
        usuario.VerificarSenha("SenhaErrada").Should().BeFalse();
    }

    [Fact]
    public void NovUsuario_DeveTermValoresPadrao()
    {
        var usuario = new Usuario();
        usuario.Role.Should().Be(ERole.Cliente);
        usuario.Ativo.Should().BeTrue();
        usuario.RefreshTokens.Should().BeEmpty();
        usuario.Agendamentos.Should().BeEmpty();
    }
}
