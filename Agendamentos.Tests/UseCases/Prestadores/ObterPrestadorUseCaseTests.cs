// ============================================================================
// Testes unitários para ObterPrestadorUseCase.
// ============================================================================

using Agendamentos.Entities.Interfaces;
using Agendamentos.Entities.Models;
using Agendamentos.Entities.UseCases.Prestadores;
using FluentAssertions;
using NSubstitute;

namespace Agendamentos.Tests.UseCases.Prestadores;

public class ObterPrestadorUseCaseTests
{
    private readonly IPrestadorRepository _prestadorRepo = Substitute.For<IPrestadorRepository>();
    private readonly ObterPrestadorUseCase _useCase;

    public ObterPrestadorUseCaseTests()
    {
        _useCase = new ObterPrestadorUseCase(_prestadorRepo);
    }

    [Fact]
    public async Task ExecutarAsync_PrestadorExiste_DeveRetornarPrestador()
    {
        // Arrange
        var prestador = new Prestador { Id = 1, Especialidade = "Dermatologia" };
        _prestadorRepo.ObterPorIdAsync(1).Returns(prestador);

        // Act
        var resultado = await _useCase.ExecutarAsync(1, CancellationToken.None);

        // Assert
        resultado.IsError.Should().BeFalse();
        resultado.Value.Should().BeSameAs(prestador);
    }

    [Fact]
    public async Task ExecutarAsync_PrestadorInexistente_DeveRetornarErro()
    {
        // Arrange
        _prestadorRepo.ObterPorIdAsync(999).Returns((Prestador?)null);

        // Act
        var resultado = await _useCase.ExecutarAsync(999, CancellationToken.None);

        // Assert
        resultado.IsError.Should().BeTrue();
        resultado.FirstError.Code.Should().Be("Prestador.NaoEncontrado");
    }
}
