// ============================================================================
// Testes unitários para ListarAgendamentosUseCase.
//
// CONCEITO IMPORTANTE (Testes de Perspectiva Dual):
// Este caso de uso tem dois caminhos distintos: perspectiva do cliente
// e perspectiva do prestador. Os testes verificam que cada caminho
// chama o repositório correto com os parâmetros corretos.
// ============================================================================

using Agendamentos.Entities.Interfaces;
using Agendamentos.Entities.Models;
using Agendamentos.Entities.UseCases.Agendamentos;
using FluentAssertions;
using NSubstitute;

namespace Agendamentos.Tests.UseCases.Agendamentos;

public class ListarAgendamentosUseCaseTests
{
    private readonly IAgendamentoRepository _agendamentoRepo = Substitute.For<IAgendamentoRepository>();
    private readonly IPrestadorRepository _prestadorRepo = Substitute.For<IPrestadorRepository>();
    private readonly ListarAgendamentosUseCase _useCase;

    public ListarAgendamentosUseCaseTests()
    {
        _useCase = new ListarAgendamentosUseCase(_agendamentoRepo, _prestadorRepo);
    }

    [Fact]
    public async Task ExecutarAsync_PerspectivaCliente_DeveBuscarPorClienteId()
    {
        // Arrange
        var agendamentos = new List<Agendamento>
        {
            new() { Id = 1, ClienteId = 10 },
            new() { Id = 2, ClienteId = 10 }
        };
        _agendamentoRepo.ListarPorClienteIdAsync(10, null, 21).Returns(agendamentos);

        // Act
        var resultado = await _useCase.ExecutarAsync(
            usuarioId: 10, ehPrestador: false,
            cursor: null, limite: 20, CancellationToken.None);

        // Assert
        resultado.IsError.Should().BeFalse();
        resultado.Value.Itens.Should().HaveCount(2);
        resultado.Value.TemMais.Should().BeFalse();

        // Não deve consultar o repositório de prestadores
        await _prestadorRepo.DidNotReceive().ObterPorUsuarioIdAsync(Arg.Any<long>());
    }

    [Fact]
    public async Task ExecutarAsync_PerspectivaPrestador_DeveBuscarPorPrestadorId()
    {
        // Arrange — o usuário 5 é prestador com PrestadorId = 1
        _prestadorRepo.ObterPorUsuarioIdAsync(5).Returns(new Prestador { Id = 1, UsuarioId = 5 });

        var agendamentos = new List<Agendamento> { new() { Id = 1, PrestadorId = 1 } };
        _agendamentoRepo.ListarPorPrestadorIdAsync(1, null, 21).Returns(agendamentos);

        // Act
        var resultado = await _useCase.ExecutarAsync(
            usuarioId: 5, ehPrestador: true,
            cursor: null, limite: 20, CancellationToken.None);

        // Assert
        resultado.IsError.Should().BeFalse();
        resultado.Value.Itens.Should().HaveCount(1);
    }

    [Fact]
    public async Task ExecutarAsync_PrestadorInexistente_DeveRetornarErro()
    {
        // Arrange — o usuário diz ser prestador, mas não tem registro
        _prestadorRepo.ObterPorUsuarioIdAsync(99).Returns((Prestador?)null);

        // Act
        var resultado = await _useCase.ExecutarAsync(
            usuarioId: 99, ehPrestador: true,
            cursor: null, limite: 20, CancellationToken.None);

        // Assert
        resultado.IsError.Should().BeTrue();
        resultado.FirstError.Code.Should().Be("Prestador.NaoEncontrado");
    }

    [Fact]
    public async Task ExecutarAsync_ComCursor_DevePassarCursorAoRepositorio()
    {
        // Arrange
        var agendamentos = new List<Agendamento>
        {
            new() { Id = 8, ClienteId = 10 },
            new() { Id = 7, ClienteId = 10 }
        };
        _agendamentoRepo.ListarPorClienteIdAsync(10, 9, 21).Returns(agendamentos);

        // Act
        var resultado = await _useCase.ExecutarAsync(
            usuarioId: 10, ehPrestador: false,
            cursor: 9, limite: 20, CancellationToken.None);

        // Assert
        resultado.IsError.Should().BeFalse();
        resultado.Value.Itens.Should().HaveCount(2);
        resultado.Value.TemMais.Should().BeFalse();
    }
}
