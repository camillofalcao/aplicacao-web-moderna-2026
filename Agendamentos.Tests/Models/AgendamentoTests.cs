using Agendamentos.Entities.Enums;
using Agendamentos.Entities.Models;
using FluentAssertions;

namespace Agendamentos.Tests.Models;

public class AgendamentoTests
{
    [Theory]
    [InlineData(EStatusAgendamento.Pendente, true)]
    [InlineData(EStatusAgendamento.Confirmado, true)]
    [InlineData(EStatusAgendamento.Cancelado, false)]
    [InlineData(EStatusAgendamento.Concluido, false)]
    public void PodeCancelar_DeveRetornarResultadoCorretoParaCadaStatus(
        EStatusAgendamento status, bool esperado)
    {
        var agendamento = new Agendamento { Status = status };
        agendamento.PodeCancelar.Should().Be(esperado);
    }

    [Fact]
    public void PertenceAoUsuario_ClienteDoAgendamento_DeveRetornarTrue()
    {
        var agendamento = new Agendamento
        {
            ClienteId = 10,
            PrestadorId = 1
        };
        agendamento.PertenceAoUsuario(usuarioId: 10, prestadorUsuarioId: 99)
            .Should().BeTrue();
    }

    [Fact]
    public void PertenceAoUsuario_PrestadorDoAgendamento_DeveRetornarTrue()
    {
        var agendamento = new Agendamento
        {
            ClienteId = 10,
            PrestadorId = 1
        };
        agendamento.PertenceAoUsuario(usuarioId: 5, prestadorUsuarioId: 5)
            .Should().BeTrue();
    }

    [Fact]
    public void PertenceAoUsuario_UsuarioNaoRelacionado_DeveRetornarFalse()
    {
        var agendamento = new Agendamento
        {
            ClienteId = 10,
            PrestadorId = 1
        };
        agendamento.PertenceAoUsuario(usuarioId: 999, prestadorUsuarioId: 5)
            .Should().BeFalse();
    }

    [Fact]
    public void Cancelar_DeveAlterarStatusParaCancelado()
    {
        var agendamento = new Agendamento { Status = EStatusAgendamento.Pendente };
        agendamento.Cancelar();
        agendamento.Status.Should().Be(EStatusAgendamento.Cancelado);
    }
}
