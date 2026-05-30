// ============================================================================
// Testes unitários para ListarUsuariosUseCase.
// ============================================================================

using Agendamentos.Entities.Interfaces;
using Agendamentos.Entities.Models;
using Agendamentos.Entities.UseCases.Usuarios;
using FluentAssertions;
using NSubstitute;

namespace Agendamentos.Tests.UseCases.Usuarios;

public class ListarUsuariosUseCaseTests
{
    private readonly IUsuarioRepository _usuarioRepo = Substitute.For<IUsuarioRepository>();
    private readonly ListarUsuariosUseCase _useCase;

    public ListarUsuariosUseCaseTests()
    {
        _useCase = new ListarUsuariosUseCase(_usuarioRepo);
    }

    [Fact]
    public async Task ExecutarAsync_DeveRetornarListaPaginada()
    {
        // Arrange — retorna 3 itens (limite+1=3, logo limite=2 → temMais=true)
        var usuarios = new List<Usuario>
        {
            new() { Id = 1, Nome = "João", Email = "joao@email.com" },
            new() { Id = 2, Nome = "Maria", Email = "maria@email.com" },
            new() { Id = 3, Nome = "Extra", Email = "extra@email.com" }
        };
        _usuarioRepo.ObterTodosAsync(null, 3).Returns(usuarios);

        // Act — limite=2, o use case passa limite+1=3 ao repo
        var resultado = await _useCase.ExecutarAsync(cursor: null, limite: 2);

        // Assert
        resultado.IsError.Should().BeFalse();
        resultado.Value.Itens.Should().HaveCount(2);
        resultado.Value.TemMais.Should().BeTrue();
        resultado.Value.ProximoCursor.Should().Be(2);
    }

    [Fact]
    public async Task ExecutarAsync_SemUsuarios_DeveRetornarListaVazia()
    {
        // Arrange
        _usuarioRepo.ObterTodosAsync(null, 21).Returns(new List<Usuario>());

        // Act
        var resultado = await _useCase.ExecutarAsync(cursor: null, limite: 20);

        // Assert
        resultado.IsError.Should().BeFalse();
        resultado.Value.Itens.Should().BeEmpty();
        resultado.Value.TemMais.Should().BeFalse();
        resultado.Value.ProximoCursor.Should().BeNull();
    }

    [Fact]
    public async Task ExecutarAsync_UltimaPagina_TemMaisDevSerFalse()
    {
        // Arrange — retorna exatamente limite itens (sem extra) → última página
        var usuarios = new List<Usuario>
        {
            new() { Id = 5, Nome = "Ana", Email = "ana@email.com" }
        };
        _usuarioRepo.ObterTodosAsync(4, 21).Returns(usuarios);

        // Act
        var resultado = await _useCase.ExecutarAsync(cursor: 4, limite: 20);

        // Assert
        resultado.IsError.Should().BeFalse();
        resultado.Value.Itens.Should().HaveCount(1);
        resultado.Value.TemMais.Should().BeFalse();
        resultado.Value.ProximoCursor.Should().BeNull();
    }
}
