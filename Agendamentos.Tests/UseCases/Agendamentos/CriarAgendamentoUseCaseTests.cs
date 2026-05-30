// ============================================================================
// Testes unitários para CriarAgendamentoUseCase.
//
// CONCEITO IMPORTANTE (Testes de Regras de Negócio Complexas):
// A criação de um agendamento possui 3 validações encadeadas:
// 1. Prestador existe e está ativo
// 2. Horário está dentro do expediente (usa HorarioDisponivel.Contem)
// 3. Não há conflito com agendamento existente
//
// Cada teste isola UM cenário de falha, garantindo que cada validação
// funciona independentemente. Isso é chamado de "one assertion per test"
// (ou pelo menos, uma RAZÃO de falha por teste).
// ============================================================================

using Agendamentos.Entities.Enums;
using Agendamentos.Entities.Interfaces;
using Agendamentos.Entities.Models;
using Agendamentos.Entities.UseCases.Agendamentos;
using FluentAssertions;
using NSubstitute;

namespace Agendamentos.Tests.UseCases.Agendamentos;

public class CriarAgendamentoUseCaseTests
{
    private readonly IAgendamentoRepository _agendamentoRepo = Substitute.For<IAgendamentoRepository>();
    private readonly IPrestadorRepository _prestadorRepo = Substitute.For<IPrestadorRepository>();
    private readonly IHorarioDisponivelRepository _horarioRepo = Substitute.For<IHorarioDisponivelRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CriarAgendamentoUseCase _useCase;

    public CriarAgendamentoUseCaseTests()
    {
        _useCase = new CriarAgendamentoUseCase(
            _agendamentoRepo, _prestadorRepo, _horarioRepo, _unitOfWork);
    }

    /// <summary>
    /// Cria um agendamento para segunda-feira às 10:00 (dentro do expediente padrão).
    /// </summary>
    private static Agendamento CriarAgendamentoValido()
    {
        // Encontra a próxima segunda-feira no futuro
        var proximaSegunda = DateTime.UtcNow.Date;
        while (proximaSegunda.DayOfWeek != DayOfWeek.Monday)
            proximaSegunda = proximaSegunda.AddDays(1);

        return new Agendamento
        {
            PrestadorId = 1,
            ClienteId = 10,
            DataHora = proximaSegunda.AddHours(10), // Segunda às 10:00
            Observacoes = "Consulta inicial"
        };
    }

    /// <summary>
    /// Configura os mocks para o cenário de sucesso (prestador ativo, horário válido, sem conflito).
    /// </summary>
    private void ConfigurarCenarioSucesso(Agendamento agendamento)
    {
        _prestadorRepo.ObterPorIdAsync(1).Returns(new Prestador { Id = 1, Ativo = true });

        _horarioRepo.ListarPorPrestadorIdAsync(1).Returns(new List<HorarioDisponivel>
        {
            new()
            {
                DiaSemana = DayOfWeek.Monday,
                HoraInicio = new TimeOnly(8, 0),
                HoraFim = new TimeOnly(18, 0)
            }
        });

        _agendamentoRepo.ExisteConflito(1, agendamento.DataHora, null).Returns(false);

        // Após salvar, a busca retorna o agendamento com dados completos
        _agendamentoRepo.ObterPorIdAsync(Arg.Any<long>()).Returns(agendamento);
    }

    [Fact]
    public async Task ExecutarAsync_DadosValidos_DeveCriarAgendamento()
    {
        // Arrange
        var agendamento = CriarAgendamentoValido();
        ConfigurarCenarioSucesso(agendamento);

        // Act
        var resultado = await _useCase.ExecutarAsync(agendamento, CancellationToken.None);

        // Assert
        resultado.IsError.Should().BeFalse();
        resultado.Value.Status.Should().Be(EStatusAgendamento.Pendente);

        await _agendamentoRepo.Received(1).AdicionarAsync(agendamento);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecutarAsync_PrestadorInexistente_DeveRetornarErro()
    {
        // Arrange
        _prestadorRepo.ObterPorIdAsync(999).Returns((Prestador?)null);
        var agendamento = CriarAgendamentoValido();
        agendamento.PrestadorId = 999;

        // Act
        var resultado = await _useCase.ExecutarAsync(agendamento, CancellationToken.None);

        // Assert
        resultado.IsError.Should().BeTrue();
        resultado.FirstError.Code.Should().Be("Prestador.NaoEncontrado");
    }

    [Fact]
    public async Task ExecutarAsync_PrestadorInativo_DeveRetornarErro()
    {
        // Arrange
        _prestadorRepo.ObterPorIdAsync(1).Returns(new Prestador { Id = 1, Ativo = false });
        var agendamento = CriarAgendamentoValido();

        // Act
        var resultado = await _useCase.ExecutarAsync(agendamento, CancellationToken.None);

        // Assert
        resultado.IsError.Should().BeTrue();
        resultado.FirstError.Code.Should().Be("Prestador.NaoEncontrado");
    }

    [Fact]
    public async Task ExecutarAsync_HorarioForaDoExpediente_DeveRetornarErro()
    {
        // Arrange — prestador existe e está ativo, MAS o horário é fora do expediente
        _prestadorRepo.ObterPorIdAsync(1).Returns(new Prestador { Id = 1, Ativo = true });
        _horarioRepo.ListarPorPrestadorIdAsync(1).Returns(new List<HorarioDisponivel>
        {
            new()
            {
                DiaSemana = DayOfWeek.Monday,
                HoraInicio = new TimeOnly(8, 0),
                HoraFim = new TimeOnly(12, 0) // Só até meio-dia
            }
        });

        // Agendamento às 14:00 — fora do expediente
        var agendamento = CriarAgendamentoValido();
        var proximaSegunda = agendamento.DataHora.Date;
        agendamento.DataHora = proximaSegunda.AddHours(14);

        // Act
        var resultado = await _useCase.ExecutarAsync(agendamento, CancellationToken.None);

        // Assert
        resultado.IsError.Should().BeTrue();
        resultado.FirstError.Code.Should().Be("Agendamento.HorarioIndisponivel");
    }

    [Fact]
    public async Task ExecutarAsync_ConflitoDeHorario_DeveRetornarErro()
    {
        // Arrange — tudo válido, mas JÁ existe agendamento neste horário
        var agendamento = CriarAgendamentoValido();

        _prestadorRepo.ObterPorIdAsync(1).Returns(new Prestador { Id = 1, Ativo = true });
        _horarioRepo.ListarPorPrestadorIdAsync(1).Returns(new List<HorarioDisponivel>
        {
            new()
            {
                DiaSemana = DayOfWeek.Monday,
                HoraInicio = new TimeOnly(8, 0),
                HoraFim = new TimeOnly(18, 0)
            }
        });
        _agendamentoRepo.ExisteConflito(1, agendamento.DataHora, null).Returns(true);

        // Act
        var resultado = await _useCase.ExecutarAsync(agendamento, CancellationToken.None);

        // Assert
        resultado.IsError.Should().BeTrue();
        resultado.FirstError.Code.Should().Be("Agendamento.ConflitoHorario");
    }
}
