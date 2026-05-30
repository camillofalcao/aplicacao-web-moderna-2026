// ============================================================================
// Testes unitários para ListarPrestadoresUseCase.
//
// CONCEITO IMPORTANTE (Testes de Cache-Aside + Paginação em Memória):
// O caso de uso implementa Cache-Aside (Lazy Loading) + paginação:
// 1. Tenta ler do cache → se encontrou (cache hit), pagina em memória
// 2. Se não encontrou (cache miss) → busca no banco, popula cache, pagina
//
// Os testes verificam AMBOS os caminhos (hit e miss), o cenário de cache
// indisponível e a paginação em memória com cursor.
// ============================================================================

using Agendamentos.Entities.Interfaces;
using Agendamentos.Entities.Models;
using Agendamentos.Entities.UseCases.Prestadores;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Agendamentos.Tests.UseCases.Prestadores;

public class ListarPrestadoresUseCaseTests
{
    private readonly IPrestadorRepository _prestadorRepo = Substitute.For<IPrestadorRepository>();
    private readonly ICachePrestadores _cache = Substitute.For<ICachePrestadores>();
    private readonly ListarPrestadoresUseCase _useCase;

    public ListarPrestadoresUseCaseTests()
    {
        _useCase = new ListarPrestadoresUseCase(_prestadorRepo, _cache);
    }

    [Fact]
    public async Task ExecutarAsync_CacheHit_DeveRetornarDoCachePaginado()
    {
        // Arrange — 3 prestadores no cache, limite 2 → página 1 com temMais=true
        var prestadoresCache = new List<Prestador>
        {
            new() { Id = 1, Especialidade = "Dermatologia", Usuario = new Usuario { Nome = "Dr. A" } },
            new() { Id = 2, Especialidade = "Cardiologia", Usuario = new Usuario { Nome = "Dr. B" } },
            new() { Id = 3, Especialidade = "Ortopedia", Usuario = new Usuario { Nome = "Dr. C" } }
        };
        _cache.ObterListaAsync(Arg.Any<CancellationToken>()).Returns(prestadoresCache);

        // Act
        var resultado = await _useCase.ExecutarAsync(
            cursor: null, limite: 2, CancellationToken.None);

        // Assert — retornou do cache sem consultar o banco
        resultado.IsError.Should().BeFalse();
        resultado.Value.Itens.Should().HaveCount(2);
        resultado.Value.TemMais.Should().BeTrue();
        resultado.Value.ProximoCursor.Should().Be(2);
        await _prestadorRepo.DidNotReceive().ListarAtivosAsync();
    }

    [Fact]
    public async Task ExecutarAsync_CacheMiss_DeveBuscarDoBancoEPopularCache()
    {
        // Arrange — cache vazio, dados estão no banco
        _cache.ObterListaAsync(Arg.Any<CancellationToken>()).Returns((List<Prestador>?)null);

        var prestadoresBanco = new List<Prestador>
        {
            new() { Id = 1, Especialidade = "Dermatologia", Usuario = new Usuario { Nome = "Dr. João" } },
            new() { Id = 2, Especialidade = "Cardiologia", Usuario = new Usuario { Nome = "Dra. Maria" } }
        };
        _prestadorRepo.ListarAtivosAsync().Returns(prestadoresBanco);

        // Act
        var resultado = await _useCase.ExecutarAsync(
            cursor: null, limite: 20, CancellationToken.None);

        // Assert — retornou do banco e populou o cache
        resultado.IsError.Should().BeFalse();
        resultado.Value.Itens.Should().HaveCount(2);
        resultado.Value.TemMais.Should().BeFalse();
        await _cache.Received(1).ArmazenarListaAsync(
            prestadoresBanco, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecutarAsync_CacheIndisponivel_DeveBuscarDoBanco()
    {
        // Arrange — Redis offline
        _cache.ObterListaAsync(Arg.Any<CancellationToken>())
            .Throws(new Exception("Redis offline"));

        var prestadoresBanco = new List<Prestador>
        {
            new() { Id = 1, Especialidade = "Ortopedia", Usuario = new Usuario { Nome = "Dr. Carlos" } }
        };
        _prestadorRepo.ListarAtivosAsync().Returns(prestadoresBanco);

        // Act
        var resultado = await _useCase.ExecutarAsync(
            cursor: null, limite: 20, CancellationToken.None);

        // Assert — retornou do banco mesmo com cache falhando
        resultado.IsError.Should().BeFalse();
        resultado.Value.Itens.Should().HaveCount(1);
    }

    [Fact]
    public async Task ExecutarAsync_ComCursor_DevePaginarAPartirDoCursor()
    {
        // Arrange — 3 prestadores no cache, cursor=1 → deve retornar Id 2 e 3
        var prestadoresCache = new List<Prestador>
        {
            new() { Id = 1, Especialidade = "Dermatologia", Usuario = new Usuario { Nome = "Dr. A" } },
            new() { Id = 2, Especialidade = "Cardiologia", Usuario = new Usuario { Nome = "Dr. B" } },
            new() { Id = 3, Especialidade = "Ortopedia", Usuario = new Usuario { Nome = "Dr. C" } }
        };
        _cache.ObterListaAsync(Arg.Any<CancellationToken>()).Returns(prestadoresCache);

        // Act — cursor=1, limite=20 → deve pular Id 1 e retornar Id 2, 3
        var resultado = await _useCase.ExecutarAsync(
            cursor: 1, limite: 20, CancellationToken.None);

        // Assert
        resultado.IsError.Should().BeFalse();
        resultado.Value.Itens.Should().HaveCount(2);
        resultado.Value.Itens[0].Id.Should().Be(2);
        resultado.Value.Itens[1].Id.Should().Be(3);
        resultado.Value.TemMais.Should().BeFalse();
    }
}
