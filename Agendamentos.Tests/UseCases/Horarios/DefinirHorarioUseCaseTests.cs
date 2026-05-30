// ============================================================================
// Testes unitários para DefinirHorarioUseCase.
// ============================================================================

using Agendamentos.Entities.Interfaces;
using Agendamentos.Entities.Models;
using Agendamentos.Entities.UseCases.Horarios;
using FluentAssertions;
using NSubstitute;

namespace Agendamentos.Tests.UseCases.Horarios;

public class DefinirHorarioUseCaseTests
{
    private readonly IHorarioDisponivelRepository _horarioRepo = Substitute.For<IHorarioDisponivelRepository>();
    private readonly IPrestadorRepository _prestadorRepo = Substitute.For<IPrestadorRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly DefinirHorarioUseCase _useCase;

    public DefinirHorarioUseCaseTests()
    {
        _useCase = new DefinirHorarioUseCase(_horarioRepo, _prestadorRepo, _unitOfWork);
    }

    [Fact]
    public async Task ExecutarAsync_PrestadorExiste_DeveDefinirHorario()
    {
        // Arrange
        _prestadorRepo.ObterPorIdAsync(1).Returns(new Prestador { Id = 1 });

        var horario = new HorarioDisponivel
        {
            PrestadorId = 1,
            DiaSemana = DayOfWeek.Monday,
            HoraInicio = new TimeOnly(8, 0),
            HoraFim = new TimeOnly(17, 0)
        };

        // Act
        var resultado = await _useCase.ExecutarAsync(horario, CancellationToken.None);

        // Assert
        resultado.IsError.Should().BeFalse();
        resultado.Value.Should().BeSameAs(horario);
        await _horarioRepo.Received(1).AdicionarAsync(horario);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecutarAsync_PrestadorInexistente_DeveRetornarErro()
    {
        // Arrange
        _prestadorRepo.ObterPorIdAsync(999).Returns((Prestador?)null);

        var horario = new HorarioDisponivel
        {
            PrestadorId = 999,
            DiaSemana = DayOfWeek.Friday,
            HoraInicio = new TimeOnly(8, 0),
            HoraFim = new TimeOnly(12, 0)
        };

        // Act
        var resultado = await _useCase.ExecutarAsync(horario, CancellationToken.None);

        // Assert
        resultado.IsError.Should().BeTrue();
        resultado.FirstError.Code.Should().Be("Prestador.NaoEncontrado");

        await _horarioRepo.DidNotReceive().AdicionarAsync(Arg.Any<HorarioDisponivel>());
    }
}
