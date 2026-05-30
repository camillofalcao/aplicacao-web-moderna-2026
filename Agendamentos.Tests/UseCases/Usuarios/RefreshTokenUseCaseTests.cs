// ============================================================================
// Testes unitários para RefreshTokenUseCase.
//
// CONCEITO IMPORTANTE (Token Rotation):
// O fluxo de refresh é um dos pontos mais críticos de segurança:
// - O token antigo DEVE ser revogado (uso único)
// - O hash do token é comparado, não o token bruto
// - Tokens expirados ou já revogados devem ser rejeitados
// - O usuário deve estar ativo
// ============================================================================

using Agendamentos.Entities.Interfaces;
using Agendamentos.Entities.Models;
using Agendamentos.Entities.Models.Auth;
using Agendamentos.Entities.UseCases.Usuarios;
using FluentAssertions;
using NSubstitute;

namespace Agendamentos.Tests.UseCases.Usuarios;

public class RefreshTokenUseCaseTests
{
    private readonly IUsuarioRepository _usuarioRepo = Substitute.For<IUsuarioRepository>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly RefreshTokenUseCase _useCase;

    // Token bruto de referência para os testes
    private const string TokenBruto = "token-bruto-de-teste";

    public RefreshTokenUseCaseTests()
    {
        _useCase = new RefreshTokenUseCase(_usuarioRepo, _tokenService, _unitOfWork);
    }

    /// <summary>
    /// Cria um usuário com um refresh token ativo (não expirado, não revogado).
    /// </summary>
    private static Usuario CriarUsuarioComTokenAtivo()
    {
        var usuario = new Usuario
        {
            Id = 1,
            Nome = "João",
            Email = "joao@email.com",
            Ativo = true,
            RefreshTokens =
            [
                new RefreshToken
                {
                    Id = 10,
                    TokenHash = RefreshToken.ComputarHash(TokenBruto),
                    ExpiraEm = DateTime.UtcNow.AddDays(7),
                    RevogadoEm = null,
                    UsuarioId = 1
                }
            ]
        };
        return usuario;
    }

    [Fact]
    public async Task ExecutarAsync_TokenValido_DeveRenovarTokensComSucesso()
    {
        // Arrange
        var tokenHash = RefreshToken.ComputarHash(TokenBruto);
        var usuario = CriarUsuarioComTokenAtivo();

        _usuarioRepo.ObterPorRefreshTokenHashAsync(tokenHash).Returns(usuario);
        _tokenService.GerarAccessToken(usuario).Returns("novo-jwt");
        _tokenService.GerarRefreshToken().Returns(("novo-refresh-bruto", new RefreshToken
        {
            TokenHash = RefreshToken.ComputarHash("novo-refresh-bruto"),
            ExpiraEm = DateTime.UtcNow.AddDays(7)
        }));

        // Act
        var resultado = await _useCase.ExecutarAsync(TokenBruto, CancellationToken.None);

        // Assert — sucesso com novos tokens
        resultado.IsError.Should().BeFalse();
        resultado.Value.AccessToken.Should().Be("novo-jwt");
        resultado.Value.RefreshToken.Should().Be("novo-refresh-bruto");

        // O token antigo deve ter sido revogado
        usuario.RefreshTokens[0].Revogado.Should().BeTrue();

        // Novo token foi adicionado ao usuário
        usuario.RefreshTokens.Should().HaveCount(2);

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecutarAsync_TokenInexistente_DeveRetornarErro()
    {
        // Arrange — nenhum usuário possui esse hash
        _usuarioRepo.ObterPorRefreshTokenHashAsync(Arg.Any<string>())
            .Returns((Usuario?)null);

        // Act
        var resultado = await _useCase.ExecutarAsync("token-inexistente", CancellationToken.None);

        // Assert
        resultado.IsError.Should().BeTrue();
        resultado.FirstError.Code.Should().Be("Auth.RefreshTokenInvalido");
    }

    [Fact]
    public async Task ExecutarAsync_TokenExpirado_DeveRetornarErro()
    {
        // Arrange — token existe mas já expirou
        var tokenHash = RefreshToken.ComputarHash(TokenBruto);
        var usuario = new Usuario
        {
            Id = 1, Nome = "João", Email = "joao@email.com", Ativo = true,
            RefreshTokens =
            [
                new RefreshToken
                {
                    TokenHash = tokenHash,
                    ExpiraEm = DateTime.UtcNow.AddMinutes(-1), // EXPIRADO
                    RevogadoEm = null
                }
            ]
        };
        _usuarioRepo.ObterPorRefreshTokenHashAsync(tokenHash).Returns(usuario);

        // Act
        var resultado = await _useCase.ExecutarAsync(TokenBruto, CancellationToken.None);

        // Assert
        resultado.IsError.Should().BeTrue();
        resultado.FirstError.Code.Should().Be("Auth.RefreshTokenInvalido");
    }

    [Fact]
    public async Task ExecutarAsync_TokenJaRevogado_DeveRetornarErro()
    {
        // Arrange — token já foi usado (revogado)
        var tokenHash = RefreshToken.ComputarHash(TokenBruto);
        var usuario = new Usuario
        {
            Id = 1, Nome = "João", Email = "joao@email.com", Ativo = true,
            RefreshTokens =
            [
                new RefreshToken
                {
                    TokenHash = tokenHash,
                    ExpiraEm = DateTime.UtcNow.AddDays(7),
                    RevogadoEm = DateTime.UtcNow.AddHours(-1) // JÁ REVOGADO
                }
            ]
        };
        _usuarioRepo.ObterPorRefreshTokenHashAsync(tokenHash).Returns(usuario);

        // Act
        var resultado = await _useCase.ExecutarAsync(TokenBruto, CancellationToken.None);

        // Assert
        resultado.IsError.Should().BeTrue();
        resultado.FirstError.Code.Should().Be("Auth.RefreshTokenInvalido");
    }

    [Fact]
    public async Task ExecutarAsync_UsuarioInativo_DeveRetornarErro()
    {
        // Arrange — token válido, mas usuário foi desativado
        var tokenHash = RefreshToken.ComputarHash(TokenBruto);
        var usuario = CriarUsuarioComTokenAtivo();
        usuario.Ativo = false; // DESATIVADO

        _usuarioRepo.ObterPorRefreshTokenHashAsync(tokenHash).Returns(usuario);

        // Act
        var resultado = await _useCase.ExecutarAsync(TokenBruto, CancellationToken.None);

        // Assert
        resultado.IsError.Should().BeTrue();
        resultado.FirstError.Code.Should().Be("Usuario.Inativo");
    }
}
