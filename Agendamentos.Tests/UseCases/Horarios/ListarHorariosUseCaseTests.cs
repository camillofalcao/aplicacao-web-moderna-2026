// ============================================================================
// Testes unitários para ListarHorariosUseCase.
// ============================================================================

using Agendamentos.Entities.Interfaces;
using Agendamentos.Entities.Models;
using Agendamentos.Entities.UseCases.Horarios;
using FluentAssertions;
using NSubstitute;

namespace Agendamentos.Tests.UseCases.Horarios;

public class ListarHorariosUseCaseTests
{
    private readonly IHorarioDisponivelRepository _horarioRepo = Substitute.For<IHorarioDisponivelRepository>();
    private readonly ListarHorariosUseCase _useCase;

    public ListarHorariosUseCaseTests()
    {
        _useCase = new ListarHorariosUseCase(_horarioRepo);
    }

    [Fact]
    public async Task ExecutarAsync_PrestadorComHorarios_DeveRetornarLista()
    {
        // Arrange
        var horarios = new List<HorarioDisponivel>
        {
            new() { Id = 1, PrestadorId = 1, DiaSemana = DayOfWeek.Monday },
            new() { Id = 2, PrestadorId = 1, DiaSemana = DayOfWeek.Wednesday }
        };
        _horarioRepo.ListarPorPrestadorIdAsync(1).Returns(horarios);

        // Act
        var resultado = await _useCase.ExecutarAsync(1, CancellationToken.None);

        // Assert
        resultado.IsError.Should().BeFalse();
        resultado.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task ExecutarAsync_PrestadorSemHorarios_DeveRetornarListaVazia()
    {
        // Arrange
        _horarioRepo.ListarPorPrestadorIdAsync(1).Returns(new List<HorarioDisponivel>());

        // Act
        var resultado = await _useCase.ExecutarAsync(1, CancellationToken.None);

        // Assert
        resultado.IsError.Should().BeFalse();
        resultado.Value.Should().BeEmpty();
    }
}
