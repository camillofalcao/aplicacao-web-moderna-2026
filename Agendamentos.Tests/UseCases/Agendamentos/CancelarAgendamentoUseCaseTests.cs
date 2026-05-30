// ============================================================================
// Testes unitários para CancelarAgendamentoUseCase.
//
// CONCEITO IMPORTANTE (Testes de Autorização a Nível de Recurso):
// O cancelamento verifica se o usuário tem permissão para cancelar
// AQUELE agendamento específico (não apenas se está autenticado).
// Isso é chamado de "resource-level authorization" ou "object-level
// authorization" — uma das falhas mais comuns em APIs (OWASP BOLA).
// ============================================================================

using Agendamentos.Entities.Enums;
using Agendamentos.Entities.Interfaces;
using Agendamentos.Entities.Models;
using Agendamentos.Entities.UseCases.Agendamentos;
using FluentAssertions;
using NSubstitute;

namespace Agendamentos.Tests.UseCases.Agendamentos;

public class CancelarAgendamentoUseCaseTests
{
    private readonly IAgendamentoRepository _agendamentoRepo = Substitute.For<IAgendamentoRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CancelarAgendamentoUseCase _useCase;

    public CancelarAgendamentoUseCaseTests()
    {
        _useCase = new CancelarAgendamentoUseCase(_agendamentoRepo, _unitOfWork);
    }

    private static Agendamento CriarAgendamentoPendente() => new()
    {
        Id = 1,
        ClienteId = 10,
        PrestadorId = 1,
        Status = EStatusAgendamento.Pendente,
        Prestador = new Prestador { Id = 1, UsuarioId = 5 }
    };

    [Fact]
    public async Task ExecutarAsync_ClienteCancelando_DeveCancelarComSucesso()
    {
        // Arrange — o cliente (ID=10) cancela seu próprio agendamento
        var agendamento = CriarAgendamentoPendente();
        _agendamentoRepo.ObterPorIdAsync(1).Returns(agendamento);

        // Act
        var resultado = await _useCase.ExecutarAsync(
            agendamentoId: 1, usuarioId: 10, CancellationToken.None);

        // Assert
        resultado.IsError.Should().BeFalse();
        resultado.Value.Status.Should().Be(EStatusAgendamento.Cancelado);
        await _agendamentoRepo.Received(1).AtualizarAsync(agendamento);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecutarAsync_PrestadorCancelando_DeveCancelarComSucesso()
    {
        // Arrange — o prestador (UsuarioId=5) cancela
        var agendamento = CriarAgendamentoPendente();
        _agendamentoRepo.ObterPorIdAsync(1).Returns(agendamento);

        // Act
        var resultado = await _useCase.ExecutarAsync(
            agendamentoId: 1, usuarioId: 5, CancellationToken.None);

        // Assert
        resultado.IsError.Should().BeFalse();
        resultado.Value.Status.Should().Be(EStatusAgendamento.Cancelado);
    }

    [Fact]
    public async Task ExecutarAsync_AgendamentoInexistente_DeveRetornarErro()
    {
        // Arrange
        _agendamentoRepo.ObterPorIdAsync(999).Returns((Agendamento?)null);

        // Act
        var resultado = await _useCase.ExecutarAsync(
            agendamentoId: 999, usuarioId: 10, CancellationToken.None);

        // Assert
        resultado.IsError.Should().BeTrue();
        resultado.FirstError.Code.Should().Be("Agendamento.NaoEncontrado");
    }

    [Fact]
    public async Task ExecutarAsync_UsuarioSemPermissao_DeveRetornarErroAcessoNegado()
    {
        // Arrange — usuário 999 não é cliente nem prestador deste agendamento
        var agendamento = CriarAgendamentoPendente();
        _agendamentoRepo.ObterPorIdAsync(1).Returns(agendamento);

        // Act
        var resultado = await _useCase.ExecutarAsync(
            agendamentoId: 1, usuarioId: 999, CancellationToken.None);

        // Assert
        resultado.IsError.Should().BeTrue();
        resultado.FirstError.Code.Should().Be("Agendamento.AcessoNegado");
    }

    [Theory]
    [InlineData(EStatusAgendamento.Cancelado)]
    [InlineData(EStatusAgendamento.Concluido)]
    public async Task ExecutarAsync_StatusNaoPermiteCancelamento_DeveRetornarErro(
        EStatusAgendamento status)
    {
        // Arrange — agendamento já cancelado ou concluído
        var agendamento = CriarAgendamentoPendente();
        agendamento.Status = status;
        _agendamentoRepo.ObterPorIdAsync(1).Returns(agendamento);

        // Act
        var resultado = await _useCase.ExecutarAsync(
            agendamentoId: 1, usuarioId: 10, CancellationToken.None);

        // Assert
        resultado.IsError.Should().BeTrue();
        resultado.FirstError.Code.Should().Be("Agendamento.NaoPodeCancelar");
    }
}
