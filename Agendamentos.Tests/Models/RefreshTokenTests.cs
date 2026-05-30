// ============================================================================
// Testes unitários para a entidade RefreshToken (Rich Domain Model).
//
// CONCEITO IMPORTANTE (Testes de Propriedades Derivadas):
// As propriedades Expirado, Revogado e Ativo são DERIVADAS de outras
// propriedades (ExpiraEm, RevogadoEm). Os testes verificam que a lógica
// de derivação está correta em cada cenário possível.
// ============================================================================

using Agendamentos.Entities.Models;
using Agendamentos.Entities.Models.Auth;
using FluentAssertions;

namespace Agendamentos.Tests.Models;

/// <summary>
/// Testes para os métodos e propriedades da entidade RefreshToken.
/// Foco: Expirado, Revogado, Ativo, Revogar() e ComputarHash().
/// </summary>
public class RefreshTokenTests
{
    // ========================================================================
    // Expirado — true se DateTime.UtcNow >= ExpiraEm
    // ========================================================================

    [Fact]
    public void Expirado_TokenComDataFutura_DeveRetornarFalse()
    {
        var token = new RefreshToken
        {
            ExpiraEm = DateTime.UtcNow.AddDays(7)
        };

        token.Expirado.Should().BeFalse();
    }

    [Fact]
    public void Expirado_TokenComDataPassada_DeveRetornarTrue()
    {
        var token = new RefreshToken
        {
            ExpiraEm = DateTime.UtcNow.AddMinutes(-1)
        };

        token.Expirado.Should().BeTrue();
    }

    // ========================================================================
    // Revogado — true se RevogadoEm != null
    // ========================================================================

    [Fact]
    public void Revogado_SemRevogacao_DeveRetornarFalse()
    {
        var token = new RefreshToken { RevogadoEm = null };

        token.Revogado.Should().BeFalse();
    }

    [Fact]
    public void Revogado_ComRevogacao_DeveRetornarTrue()
    {
        var token = new RefreshToken { RevogadoEm = DateTime.UtcNow };

        token.Revogado.Should().BeTrue();
    }

    // ========================================================================
    // Ativo — !Expirado && !Revogado
    // ========================================================================

    [Fact]
    public void Ativo_TokenValidoENaoRevogado_DeveRetornarTrue()
    {
        var token = new RefreshToken
        {
            ExpiraEm = DateTime.UtcNow.AddDays(7),
            RevogadoEm = null
        };

        token.Ativo.Should().BeTrue();
    }

    [Fact]
    public void Ativo_TokenExpirado_DeveRetornarFalse()
    {
        var token = new RefreshToken
        {
            ExpiraEm = DateTime.UtcNow.AddMinutes(-1),
            RevogadoEm = null
        };

        token.Ativo.Should().BeFalse();
    }

    [Fact]
    public void Ativo_TokenRevogado_DeveRetornarFalse()
    {
        var token = new RefreshToken
        {
            ExpiraEm = DateTime.UtcNow.AddDays(7),
            RevogadoEm = DateTime.UtcNow
        };

        token.Ativo.Should().BeFalse();
    }

    // ========================================================================
    // Revogar — marca RevogadoEm com DateTime.UtcNow
    // ========================================================================

    [Fact]
    public void Revogar_DevePreencherRevogadoEm()
    {
        // Arrange
        var token = new RefreshToken
        {
            ExpiraEm = DateTime.UtcNow.AddDays(7)
        };

        // Act
        token.Revogar();

        // Assert
        token.RevogadoEm.Should().NotBeNull();
        token.RevogadoEm.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        token.Revogado.Should().BeTrue();
        token.Ativo.Should().BeFalse();
    }

    // ========================================================================
    // ComputarHash — gera hash SHA256 em Base64 a partir do token bruto
    // ========================================================================

    [Fact]
    public void ComputarHash_DeveRetornarHashConsistente()
    {
        // O mesmo input deve sempre gerar o mesmo hash (determinístico)
        var tokenBruto = "meu-token-de-teste-12345";

        var hash1 = RefreshToken.ComputarHash(tokenBruto);
        var hash2 = RefreshToken.ComputarHash(tokenBruto);

        hash1.Should().Be(hash2);
    }

    [Fact]
    public void ComputarHash_DeveRetornarHashDiferenteParaInputsDiferentes()
    {
        var hash1 = RefreshToken.ComputarHash("token-A");
        var hash2 = RefreshToken.ComputarHash("token-B");

        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void ComputarHash_NaoDeveRetornarOTokenOriginal()
    {
        var tokenBruto = "meu-token-secreto";
        var hash = RefreshToken.ComputarHash(tokenBruto);

        hash.Should().NotBe(tokenBruto);
    }

    [Fact]
    public void ComputarHash_DeveRetornarBase64Valido()
    {
        var hash = RefreshToken.ComputarHash("qualquer-token");

        // Base64 válido: pode ser convertido de volta para bytes sem erro
        var act = () => Convert.FromBase64String(hash);
        act.Should().NotThrow();
    }
}
