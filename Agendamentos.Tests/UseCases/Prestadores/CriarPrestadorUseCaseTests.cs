// ============================================================================
// Testes unitários para CriarPrestadorUseCase.
//
// CONCEITO IMPORTANTE (Testes de Operações Atômicas):
// A criação de um prestador envolve múltiplas operações que devem acontecer
// juntas (atomicamente): criar o prestador, alterar a Role do usuário e
// invalidar o cache. Os testes verificam que todas as etapas ocorrem
// na ordem correta e que falhas intermediárias são tratadas.
// ============================================================================

using Agendamentos.Entities.Enums;
using Agendamentos.Entities.Interfaces;
using Agendamentos.Entities.Models;
using Agendamentos.Entities.UseCases.Prestadores;
using FluentAssertions;
using NSubstitute;

namespace Agendamentos.Tests.UseCases.Prestadores;

public class CriarPrestadorUseCaseTests
{
    private readonly IUsuarioRepository _usuarioRepo = Substitute.For<IUsuarioRepository>();
    private readonly IPrestadorRepository _prestadorRepo = Substitute.For<IPrestadorRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICachePrestadores _cache = Substitute.For<ICachePrestadores>();
    private readonly CriarPrestadorUseCase _useCase;

    public CriarPrestadorUseCaseTests()
    {
        _useCase = new CriarPrestadorUseCase(
            _usuarioRepo, _prestadorRepo, _unitOfWork, _cache);
    }

    private static Prestador CriarPrestadorValido() => new()
    {
        UsuarioId = 1,
        Especialidade = "Dermatologia",
        Descricao = "Especialista em dermatologia clínica e estética",
        ValorConsulta = 200m,
        DuracaoAtendimentoMinutos = 30
    };

    [Fact]
    public async Task ExecutarAsync_DadosValidos_DeveCriarPrestadorComSucesso()
    {
        // Arrange
        var usuario = new Usuario { Id = 1, Nome = "Dr. João", Email = "dr@email.com" };
        _usuarioRepo.ObterPorIdAsync(1).Returns(usuario);
        _prestadorRepo.ObterPorUsuarioIdAsync(1).Returns((Prestador?)null);

        var prestador = CriarPrestadorValido();

        // Act
        var resultado = await _useCase.ExecutarAsync(prestador, CancellationToken.None);

        // Assert
        resultado.IsError.Should().BeFalse();
        resultado.Value.Ativo.Should().BeTrue();
        resultado.Value.Usuario.Should().BeSameAs(usuario);

        // A Role do usuário deve ter sido alterada para Prestador
        usuario.Role.Should().Be(ERole.Prestador);

        // Verificações de chamadas
        await _prestadorRepo.Received(1).AdicionarAsync(prestador);
        await _usuarioRepo.Received(1).AtualizarAsync(usuario);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _cache.Received(1).InvalidarAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecutarAsync_UsuarioInexistente_DeveRetornarErro()
    {
        // Arrange
        _usuarioRepo.ObterPorIdAsync(999).Returns((Usuario?)null);
        var prestador = CriarPrestadorValido();
        prestador.UsuarioId = 999;

        // Act
        var resultado = await _useCase.ExecutarAsync(prestador, CancellationToken.None);

        // Assert
        resultado.IsError.Should().BeTrue();
        resultado.FirstError.Code.Should().Be("Usuario.NaoEncontrado");
    }

    [Fact]
    public async Task ExecutarAsync_UsuarioJaEhPrestador_DeveRetornarErroConflito()
    {
        // Arrange — usuário existe E já é prestador
        var usuario = new Usuario { Id = 1, Nome = "Dr. João" };
        _usuarioRepo.ObterPorIdAsync(1).Returns(usuario);
        _prestadorRepo.ObterPorUsuarioIdAsync(1).Returns(new Prestador { Id = 10 });

        var prestador = CriarPrestadorValido();

        // Act
        var resultado = await _useCase.ExecutarAsync(prestador, CancellationToken.None);

        // Assert
        resultado.IsError.Should().BeTrue();
        resultado.FirstError.Code.Should().Be("Prestador.UsuarioJaEhPrestador");
    }

    [Fact]
    public async Task ExecutarAsync_FalhaNoCache_NaoDeveImpedirCriacao()
    {
        // Arrange — cache lança exceção, mas a criação deve continuar
        var usuario = new Usuario { Id = 1, Nome = "Dr. João", Email = "dr@email.com" };
        _usuarioRepo.ObterPorIdAsync(1).Returns(usuario);
        _prestadorRepo.ObterPorUsuarioIdAsync(1).Returns((Prestador?)null);
        _cache.InvalidarAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new Exception("Redis offline")));

        var prestador = CriarPrestadorValido();

        // Act
        var resultado = await _useCase.ExecutarAsync(prestador, CancellationToken.None);

        // Assert — sucesso apesar do cache ter falhado
        resultado.IsError.Should().BeFalse();
    }
}
