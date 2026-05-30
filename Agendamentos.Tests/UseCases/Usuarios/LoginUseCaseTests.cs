// ============================================================================
// Testes unitários para LoginUseCase.
//
// CONCEITO IMPORTANTE (Testes de Segurança):
// Os testes de login verificam cenários críticos de segurança:
// 1. Credenciais válidas → gera tokens
// 2. E-mail inexistente → erro genérico (sem vazamento de informação)
// 3. Senha incorreta → MESMO erro genérico (impede enumeração de e-mails)
// 4. Usuário inativo → erro de acesso negado
//
// A mensagem de erro genérica ("E-mail ou senha inválidos") é proposital
// e verificada nos testes para garantir que nunca revele se o e-mail existe.
// ============================================================================

using Agendamentos.Entities.Interfaces;
using Agendamentos.Entities.Models;
using Agendamentos.Entities.Models.Auth;
using Agendamentos.Entities.UseCases.Usuarios;
using FluentAssertions;
using NSubstitute;

namespace Agendamentos.Tests.UseCases.Usuarios;

public class LoginUseCaseTests
{
    private readonly IUsuarioRepository _usuarioRepo = Substitute.For<IUsuarioRepository>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly LoginUseCase _useCase;

    public LoginUseCaseTests()
    {
        _useCase = new LoginUseCase(_usuarioRepo, _tokenService, _unitOfWork);
    }

    /// <summary>
    /// Cria um usuário válido com senha já hasheada para uso nos testes.
    /// </summary>
    private static Usuario CriarUsuarioValido()
    {
        var usuario = new Usuario
        {
            Id = 1,
            Nome = "João",
            Email = "joao@email.com",
            Ativo = true
        };
        usuario.DefinirSenha("Senh@123");
        return usuario;
    }

    [Fact]
    public async Task ExecutarAsync_CredenciaisValidas_DeveRetornarResultadoLogin()
    {
        // Arrange
        var usuario = CriarUsuarioValido();
        _usuarioRepo.ObterPorEmailAsync("joao@email.com").Returns(usuario);
        _tokenService.GerarAccessToken(usuario).Returns("jwt-token-fake");
        _tokenService.GerarRefreshToken().Returns(("raw-refresh-token", new RefreshToken
        {
            TokenHash = RefreshToken.ComputarHash("raw-refresh-token"),
            ExpiraEm = DateTime.UtcNow.AddDays(7)
        }));

        // Act
        var resultado = await _useCase.ExecutarAsync("joao@email.com", "Senh@123", CancellationToken.None);

        // Assert
        resultado.IsError.Should().BeFalse();
        resultado.Value.AccessToken.Should().Be("jwt-token-fake");
        resultado.Value.RefreshToken.Should().Be("raw-refresh-token");
        resultado.Value.Usuario.Should().BeSameAs(usuario);

        // Verifica que o refresh token foi adicionado ao usuário e salvo
        usuario.RefreshTokens.Should().HaveCount(1);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecutarAsync_EmailInexistente_DeveRetornarErroSenhaInvalida()
    {
        // Arrange — nenhum usuário com esse e-mail
        _usuarioRepo.ObterPorEmailAsync("naoexiste@email.com").Returns((Usuario?)null);

        // Act
        var resultado = await _useCase.ExecutarAsync("naoexiste@email.com", "qualquer", CancellationToken.None);

        // Assert — mensagem GENÉRICA (não revela que o e-mail não existe)
        resultado.IsError.Should().BeTrue();
        resultado.FirstError.Code.Should().Be("Usuario.SenhaInvalida");
    }

    [Fact]
    public async Task ExecutarAsync_SenhaIncorreta_DeveRetornarErroSenhaInvalida()
    {
        // Arrange — usuário existe mas senha está errada
        var usuario = CriarUsuarioValido();
        _usuarioRepo.ObterPorEmailAsync("joao@email.com").Returns(usuario);

        // Act
        var resultado = await _useCase.ExecutarAsync("joao@email.com", "SenhaErrada", CancellationToken.None);

        // Assert — MESMA mensagem genérica do cenário de e-mail inexistente
        resultado.IsError.Should().BeTrue();
        resultado.FirstError.Code.Should().Be("Usuario.SenhaInvalida");
    }

    [Fact]
    public async Task ExecutarAsync_UsuarioInativo_DeveRetornarErroInativo()
    {
        // Arrange — senha correta, mas usuário desativado
        var usuario = CriarUsuarioValido();
        usuario.Ativo = false;
        _usuarioRepo.ObterPorEmailAsync("joao@email.com").Returns(usuario);

        // Act
        var resultado = await _useCase.ExecutarAsync("joao@email.com", "Senh@123", CancellationToken.None);

        // Assert
        resultado.IsError.Should().BeTrue();
        resultado.FirstError.Code.Should().Be("Usuario.Inativo");
    }

    [Fact]
    public async Task ExecutarAsync_SenhaErrada_NaoDeveGerarTokens()
    {
        // Arrange
        var usuario = CriarUsuarioValido();
        _usuarioRepo.ObterPorEmailAsync("joao@email.com").Returns(usuario);

        // Act
        await _useCase.ExecutarAsync("joao@email.com", "SenhaErrada", CancellationToken.None);

        // Assert — NÃO deve gerar tokens nem salvar nada
        _tokenService.DidNotReceive().GerarAccessToken(Arg.Any<Usuario>());
        _tokenService.DidNotReceive().GerarRefreshToken();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
