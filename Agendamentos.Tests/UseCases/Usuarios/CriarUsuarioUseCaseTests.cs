// ============================================================================
// Testes unitários para CriarUsuarioUseCase.
//
// CONCEITO IMPORTANTE (Testes com Mocks — NSubstitute):
// Em testes unitários de casos de uso, NÃO queremos acessar o banco de dados
// real. Usamos "mocks" (substitutos) das interfaces de repositório.
//
// O NSubstitute cria implementações falsas das interfaces:
//   var repo = Substitute.For<IUsuarioRepository>();
//
// Podemos configurar o comportamento esperado:
//   repo.ObterPorEmailAsync("teste@email.com").Returns((Usuario?)null);
//
// E verificar se os métodos foram chamados corretamente:
//   await repo.Received(1).AdicionarAsync(Arg.Any<Usuario>());
//
// CONCEITO IMPORTANTE (Arrange → Act → Assert):
// Padrão universal de estruturação de testes unitários:
// 1. ARRANGE — preparar o cenário (criar objetos, configurar mocks)
// 2. ACT — executar a ação sendo testada
// 3. ASSERT — verificar o resultado
// ============================================================================

using Agendamentos.Entities.Enums;
using Agendamentos.Entities.Interfaces;
using Agendamentos.Entities.Models;
using Agendamentos.Entities.UseCases.Usuarios;
using FluentAssertions;
using NSubstitute;

namespace Agendamentos.Tests.UseCases.Usuarios;

public class CriarUsuarioUseCaseTests
{
    // Dependências mockadas — compartilhadas entre todos os testes da classe
    private readonly IUsuarioRepository _usuarioRepo = Substitute.For<IUsuarioRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CriarUsuarioUseCase _useCase;

    public CriarUsuarioUseCaseTests()
    {
        _useCase = new CriarUsuarioUseCase(_usuarioRepo, _unitOfWork);
    }

    [Fact]
    public async Task ExecutarAsync_DadosValidos_DeveCriarUsuarioComSucesso()
    {
        // Arrange — e-mail não existe no banco
        _usuarioRepo.ObterPorEmailAsync("joao@email.com").Returns((Usuario?)null);

        var usuario = new Usuario { Nome = "João", Email = "joao@email.com" };

        // Act
        var resultado = await _useCase.ExecutarAsync(usuario, "Senh@123", CancellationToken.None);

        // Assert — resultado é sucesso e a entidade foi configurada corretamente
        resultado.IsError.Should().BeFalse();
        resultado.Value.Should().BeSameAs(usuario);
        resultado.Value.Role.Should().Be(ERole.Cliente);
        resultado.Value.Ativo.Should().BeTrue();
        resultado.Value.SenhaHash.Should().NotBeNullOrWhiteSpace();

        // Verifica que o repositório e o UoW foram chamados
        await _usuarioRepo.Received(1).AdicionarAsync(usuario);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecutarAsync_EmailJaExiste_DeveRetornarErroConflito()
    {
        // Arrange — o e-mail JÁ existe no banco
        var existente = new Usuario { Id = 1, Email = "joao@email.com" };
        _usuarioRepo.ObterPorEmailAsync("joao@email.com").Returns(existente);

        var novoUsuario = new Usuario { Nome = "João", Email = "joao@email.com" };

        // Act
        var resultado = await _useCase.ExecutarAsync(novoUsuario, "Senh@123", CancellationToken.None);

        // Assert — deve retornar erro de conflito (e-mail duplicado)
        resultado.IsError.Should().BeTrue();
        resultado.FirstError.Code.Should().Be("Usuario.EmailJaExiste");

        // Verifica que NÃO tentou adicionar nem salvar
        await _usuarioRepo.DidNotReceive().AdicionarAsync(Arg.Any<Usuario>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecutarAsync_DeveDefinirSenhaHashViaDominio()
    {
        // Arrange
        _usuarioRepo.ObterPorEmailAsync(Arg.Any<string>()).Returns((Usuario?)null);
        var usuario = new Usuario { Nome = "Maria", Email = "maria@email.com" };

        // Act
        await _useCase.ExecutarAsync(usuario, "MinhaSenh@Forte", CancellationToken.None);

        // Assert — o hash BCrypt foi gerado (começa com "$2")
        usuario.SenhaHash.Should().StartWith("$2");
        usuario.VerificarSenha("MinhaSenh@Forte").Should().BeTrue();
    }

    [Fact]
    public async Task ExecutarAsync_DeveDefinirCriadoEmComoUtcNow()
    {
        // Arrange
        _usuarioRepo.ObterPorEmailAsync(Arg.Any<string>()).Returns((Usuario?)null);
        var usuario = new Usuario { Nome = "Carlos", Email = "carlos@email.com" };
        var antes = DateTime.UtcNow;

        // Act
        await _useCase.ExecutarAsync(usuario, "Senh@123", CancellationToken.None);

        // Assert
        usuario.CriadoEm.Should().BeOnOrAfter(antes);
        usuario.CriadoEm.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }
}
