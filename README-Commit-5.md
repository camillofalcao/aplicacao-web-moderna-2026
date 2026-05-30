# 📦 Parte 5 — Casos de Uso e Validators (TDD: teste primeiro, implementação em seguida)

Esta parte segue o ciclo do TDD de ponta a ponta. Para **cada** funcionalidade,
escrevemos primeiro o **teste** (que falha porque a implementação ainda não
existe) e, **em seguida**, implementamos o **caso de uso** e o **validator** que
fazem o teste passar. Trabalhamos uma funcionalidade de cada vez, no ritmo
clássico do TDD: **Red → Green**.

> Se você tentar compilar logo após escrever um teste, vai dar erro — e isso é
> esperado! O passo seguinte (o caso de uso / validator) resolve o erro.

**Conceitos utilizados nos testes:**
- **NSubstitute** — cria implementações falsas (mocks) das interfaces de repositório
- **Arrange / Act / Assert** — padrao universal de estruturacao de testes
- **ErrorOr** — verificacao de resultados de sucesso/erro sem excecoes
- **FluentValidation** — regras de validacao de formato testadas isoladamente

Crie as pastas para os testes e para as implementações:

```bash
# Testes
mkdir -p Agendamentos.Tests/UseCases/Usuarios
mkdir -p Agendamentos.Tests/UseCases/Prestadores
mkdir -p Agendamentos.Tests/UseCases/Agendamentos
mkdir -p Agendamentos.Tests/UseCases/Horarios
mkdir -p Agendamentos.Tests/Validators

# Implementações (as pastas já podem existir desde a Parte 1)
mkdir -p Agendamentos.Entities/UseCases/Usuarios
mkdir -p Agendamentos.Entities/UseCases/Prestadores
mkdir -p Agendamentos.Entities/UseCases/Agendamentos
mkdir -p Agendamentos.Entities/UseCases/Horarios
```

Garanta que os pacotes usados pelos testes estão instalados no projeto de
testes. Eles já foram adicionados na Parte 4, mas inclua aqui caso esteja
começando por esta parte:

```bash
dotnet add Agendamentos.Tests package NSubstitute --version 5.3.0
dotnet add Agendamentos.Tests package FluentAssertions --version 8.9.0
```

## 5.1 Usuários — Criar usuário (auto-cadastro)

### 5.1.1 Teste: CriarUsuarioUseCaseTests.cs

Testes para a criacao de usuario: cenario de sucesso, e-mail duplicado, hash de
senha e data de criacao.

**Arquivo: `Agendamentos.Tests/UseCases/Usuarios/CriarUsuarioUseCaseTests.cs`**

```csharp
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
```

### 5.1.2 Caso de uso: CriarUsuarioUseCase.cs

O primeiro caso de uso a implementar. Encapsula a logica de criacao de usuario
usando o padrao Rich Domain Model (a entidade Usuario faz o hash da senha).

**Arquivo: `Agendamentos.Entities/UseCases/Usuarios/CriarUsuarioUseCase.cs`**

```csharp
// ============================================================================
// Arquivo: UseCases/Usuarios/CriarUsuarioUseCase.cs
// Descrição: Caso de uso para criação de um novo usuário no sistema.
//
// CONCEITO IMPORTANTE (Use Case Pattern):
// Um caso de uso é uma classe instanciável que encapsula uma operação
// de negócio completa. Diferente do padrão Handler estático, o caso de uso:
// 1. É instanciável — pode ser registrado no container de DI
// 2. Recebe dependências via CONSTRUTOR — o ASP.NET Core resolve automaticamente
// 3. Recebe a ENTIDADE como parâmetro do método ExecutarAsync
// 4. Retorna a ENTIDADE de domínio — o mapeamento para DTO é feito no endpoint
//
// CONCEITO IMPORTANTE (Rich Domain Model):
// A entidade Usuario possui o método DefinirSenha() que encapsula a lógica
// de hashing BCrypt. O caso de uso usa esse método em vez de chamar
// BCrypt diretamente, respeitando o encapsulamento do domínio.
// ============================================================================

using Agendamentos.Entities.Enums;
using Agendamentos.Entities.Interfaces;
using Agendamentos.Entities.Models;
using Agendamentos.Entities.UseCases.Common;
using ErrorOr;

namespace Agendamentos.Entities.UseCases.Usuarios;

/// <summary>
/// Caso de uso responsável por criar um novo usuário (auto-cadastro de clientes).
///
/// Fluxo de execução:
/// 1. Verifica se já existe um usuário com o mesmo e-mail (regra de negócio)
/// 2. Usa o método DefinirSenha() da entidade para gerar o hash BCrypt
/// 3. Configura a entidade Usuario com ERole.Cliente (auto-cadastro)
/// 4. Persiste no banco de dados via repositório + Unit of Work
/// 5. Retorna a entidade Usuario criada (o endpoint mapeia para DTO)
/// </summary>
public class CriarUsuarioUseCase
{
    // ---------------------------------------------------------------
    // Dependências injetadas via construtor
    // ---------------------------------------------------------------
    // Campos readonly garantem que as dependências não sejam alteradas
    // após a construção do objeto (imutabilidade).
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Construtor com injeção de dependência.
    ///
    /// O container de DI do ASP.NET Core resolve automaticamente
    /// essas dependências quando o caso de uso é instanciado.
    /// Como o caso de uso é registrado como Scoped, ele recebe
    /// as mesmas instâncias de repositório e UoW da requisição atual.
    /// </summary>
    /// <param name="usuarioRepository">Repositório para acesso a dados de usuários.</param>
    /// <param name="unitOfWork">Unit of Work para salvar as alterações no banco.</param>
    public CriarUsuarioUseCase(
        IUsuarioRepository usuarioRepository,
        IUnitOfWork unitOfWork)
    {
        _usuarioRepository = usuarioRepository;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Executa a criação de um novo usuário no sistema.
    ///
    /// O endpoint da API cria a entidade Usuario com Nome e Email,
    /// e passa junto a senha em texto plano. O caso de uso é responsável
    /// por fazer o hash da senha (via entidade) e configurar os valores padrão.
    /// </summary>
    /// <param name="usuario">
    ///     Entidade Usuario com Nome e Email preenchidos pelo endpoint.
    ///     Os demais campos (SenhaHash, Role, Ativo, CriadoEm) são
    ///     configurados por este caso de uso.
    /// </param>
    /// <param name="senha">
    ///     Senha em texto plano informada pelo usuário.
    ///     Será convertida em hash BCrypt pela entidade (DefinirSenha).
    ///     A senha em texto plano NUNCA é persistida no banco de dados.
    /// </param>
    /// <param name="ct">Token de cancelamento da operação.</param>
    /// <returns>
    /// ErrorOr com a entidade Usuario em caso de sucesso,
    /// ou Error em caso de falha (ex: e-mail duplicado).
    /// </returns>
    public async Task<ErrorOr<Usuario>> ExecutarAsync(
        Usuario usuario,
        string senha,
        CancellationToken ct)
    {
        // ---------------------------------------------------------------
        // Passo 1: Verificar se o e-mail já está cadastrado
        // ---------------------------------------------------------------
        // Buscamos no banco um usuário com o mesmo e-mail.
        // Se encontrarmos, retornamos um erro de conflito (HTTP 409).
        // Isso garante a unicidade do e-mail como credencial de login.
        var existente = await _usuarioRepository.ObterPorEmailAsync(usuario.Email);

        if (existente is not null)
            return Erros.Usuario.EmailJaExiste;

        // ---------------------------------------------------------------
        // Passo 2: Gerar o hash da senha usando Rich Domain Model
        // ---------------------------------------------------------------
        // A entidade Usuario encapsula a lógica de hashing BCrypt
        // no método DefinirSenha(). Isso mantém a regra de negócio
        // (nunca armazenar senha em texto plano) dentro da entidade.
        usuario.DefinirSenha(senha);

        // ---------------------------------------------------------------
        // Passo 3: Configurar valores padrão para auto-cadastro
        // ---------------------------------------------------------------
        // Auto-cadastro sempre cria um cliente (ERole.Cliente).
        // O administrador pode promover o cliente a prestador posteriormente.
        usuario.Role = ERole.Cliente;
        usuario.Ativo = true;
        usuario.CriadoEm = DateTime.UtcNow;

        // ---------------------------------------------------------------
        // Passo 4: Persistir no banco de dados
        // ---------------------------------------------------------------
        // AdicionarAsync marca a entidade como "Added" no EF Core.
        // SaveChangesAsync efetiva a inserção no banco (INSERT INTO ...).
        await _usuarioRepository.AdicionarAsync(usuario);
        await _unitOfWork.SaveChangesAsync(ct);

        // ---------------------------------------------------------------
        // Passo 5: Retornar a entidade criada
        // ---------------------------------------------------------------
        // O endpoint da API é responsável por converter a entidade
        // em DTO, selecionando apenas os campos públicos.
        return usuario;
    }
}
```

### 5.1.3 Teste do Validator: CriarUsuarioValidatorTests.cs

Testes para as regras de validacao do usuario: nome obrigatorio (minimo 3 caracteres)
e e-mail obrigatorio (formato valido).

**Arquivo: `Agendamentos.Tests/Validators/CriarUsuarioValidatorTests.cs`**

```csharp
// ============================================================================
// Testes unitários para CriarUsuarioValidator (FluentValidation).
//
// CONCEITO IMPORTANTE (Testes de Validação):
// Os validators do FluentValidation são classes que definem regras de
// validação para entidades. Os testes verificam que:
// 1. Dados VÁLIDOS passam na validação (sem erros)
// 2. Cada campo INVÁLIDO gera a mensagem de erro correta
//
// O FluentValidation fornece um TestHelper, mas usamos a abordagem
// direta (chamar ValidateAsync) por ser mais explícita e didática.
// ============================================================================

using Agendamentos.Entities.Models;
using Agendamentos.Entities.UseCases.Usuarios;
using FluentAssertions;

namespace Agendamentos.Tests.Validators;

public class CriarUsuarioValidatorTests
{
    private readonly CriarUsuarioValidator _validator = new();

    [Fact]
    public async Task Validar_DadosValidos_DevePassarSemErros()
    {
        // Arrange
        var usuario = new Usuario { Nome = "João Silva", Email = "joao@email.com" };

        // Act
        var resultado = await _validator.ValidateAsync(usuario);

        // Assert
        resultado.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Validar_NomeVazio_DeveRetornarErro(string? nome)
    {
        var usuario = new Usuario { Nome = nome!, Email = "joao@email.com" };

        var resultado = await _validator.ValidateAsync(usuario);

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == "Nome");
    }

    [Fact]
    public async Task Validar_NomeMenorQue3Caracteres_DeveRetornarErro()
    {
        var usuario = new Usuario { Nome = "Ab", Email = "joao@email.com" };

        var resultado = await _validator.ValidateAsync(usuario);

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e =>
            e.PropertyName == "Nome" &&
            e.ErrorMessage.Contains("3 caracteres"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Validar_EmailVazio_DeveRetornarErro(string? email)
    {
        var usuario = new Usuario { Nome = "João Silva", Email = email! };

        var resultado = await _validator.ValidateAsync(usuario);

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Theory]
    [InlineData("email-sem-arroba")]
    [InlineData("email@")]
    [InlineData("@dominio.com")]
    public async Task Validar_EmailInvalido_DeveRetornarErro(string email)
    {
        var usuario = new Usuario { Nome = "João Silva", Email = email };

        var resultado = await _validator.ValidateAsync(usuario);

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e =>
            e.PropertyName == "Email" &&
            e.ErrorMessage.Contains("válido"));
    }
}
```

### 5.1.4 Validator: CriarUsuarioValidator.cs

Validador FluentValidation para a entidade Usuario. Define regras de formato
(nome obrigatorio, e-mail valido). A validacao de regras de negocio (e-mail
duplicado) e feita no caso de uso.

Antes, instale o FluentValidarion no projeto Agendamentos.Entities:

```bash
dotnet add package FluentValidation
```

**Arquivo: `Agendamentos.Entities/UseCases/Usuarios/CriarUsuarioValidator.cs`**

```csharp
// ============================================================================
// Arquivo: UseCases/Usuarios/CriarUsuarioValidator.cs
// Descrição: Validador FluentValidation para a entidade Usuario (criação).
//
// CONCEITO IMPORTANTE (FluentValidation):
// FluentValidation é uma biblioteca que permite definir regras de validação
// usando uma API fluente (encadeamento de métodos). Vantagens:
// 1. Separação de responsabilidades: a validação fica fora do caso de uso
// 2. Regras expressivas e legíveis ("RuleFor(x => x.Nome).NotEmpty()")
// 3. Mensagens de erro customizáveis
// 4. Fácil de testar isoladamente
//
// CONCEITO IMPORTANTE (Validação vs. Regra de Negócio):
// - Validação (feita aqui): formato dos dados (campo obrigatório, tamanho)
// - Regra de negócio (feita no caso de uso): lógica do domínio (e-mail já existe?)
// A validação é "barata" e acontece antes de acessar o banco de dados.
//
// NOTA: A senha é validada no endpoint porque não faz parte da entidade
// Usuario (a entidade armazena SenhaHash, não a senha em texto plano).
// ============================================================================

using Agendamentos.Entities.Models;
using FluentValidation;

namespace Agendamentos.Entities.UseCases.Usuarios;

/// <summary>
/// Validador que define as regras de validação para a entidade <see cref="Usuario"/>.
///
/// Herda de AbstractValidator&lt;Usuario&gt; que é a classe base do FluentValidation.
/// As regras são definidas no construtor usando a API fluente.
///
/// O endpoint injeta IValidator&lt;Usuario&gt; e chama ValidateAsync antes
/// de chamar o caso de uso. Se a validação falhar, o caso de uso NÃO é executado.
/// </summary>
public class CriarUsuarioValidator : AbstractValidator<Usuario>
{
    public CriarUsuarioValidator()
    {
        // ---------------------------------------------------------------
        // Regras para o campo Nome
        // ---------------------------------------------------------------
        // NotEmpty(): verifica se NÃO é null, vazio ("") ou whitespace ("   ")
        // MinimumLength(3): verifica se tem pelo menos 3 caracteres
        RuleFor(u => u.Nome)
            .NotEmpty()
                .WithMessage("O nome é obrigatório.")
            .MinimumLength(3)
                .WithMessage("O nome deve ter pelo menos 3 caracteres.");

        // ---------------------------------------------------------------
        // Regras para o campo Email
        // ---------------------------------------------------------------
        // NotEmpty(): campo obrigatório
        // EmailAddress(): valida o formato de e-mail (contém @, domínio, etc.)
        RuleFor(u => u.Email)
            .NotEmpty()
                .WithMessage("O e-mail é obrigatório.")
            .EmailAddress()
                .WithMessage("O e-mail informado não é válido.");
    }
}
```

## 5.2 Usuários — Login

### 5.2.1 Teste: LoginUseCaseTests.cs

Testes para o login: credenciais validas, e-mail inexistente, senha incorreta,
usuario inativo. Note como a mensagem de erro e propositalmente generica para
evitar enumeracao de e-mails.

**Arquivo: `Agendamentos.Tests/UseCases/Usuarios/LoginUseCaseTests.cs`**

```csharp
// ============================================================================
// Testes unitários para LoginUseCase.
//
// CONCEITO IMPORTANTE (Testes de Segurança):
// Os testes de login verificam cenários críticos de segurança:
// 1. Credenciais válidas → gera tokens
// 2. E-mail inexistente → erro genérico (sem vazamento de informação)
// 3. Senha incorreta → MESMO erro genérico (impede enumeração de e-mails)
// 4. Usuário inativo → erro de acesso negado
//
// A mensagem de erro genérica ("E-mail ou senha inválidos") é proposital
// e verificada nos testes para garantir que nunca revele se o e-mail existe.
// ============================================================================

using Agendamentos.Entities.Interfaces;
using Agendamentos.Entities.Models;
using Agendamentos.Entities.Models.Auth;
using Agendamentos.Entities.UseCases.Usuarios;
using FluentAssertions;
using NSubstitute;

namespace Agendamentos.Tests.UseCases.Usuarios;

public class LoginUseCaseTests
{
    private readonly IUsuarioRepository _usuarioRepo = Substitute.For<IUsuarioRepository>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly LoginUseCase _useCase;

    public LoginUseCaseTests()
    {
        _useCase = new LoginUseCase(_usuarioRepo, _tokenService, _unitOfWork);
    }

    /// <summary>
    /// Cria um usuário válido com senha já hasheada para uso nos testes.
    /// </summary>
    private static Usuario CriarUsuarioValido()
    {
        var usuario = new Usuario
        {
            Id = 1,
            Nome = "João",
            Email = "joao@email.com",
            Ativo = true
        };
        usuario.DefinirSenha("Senh@123");
        return usuario;
    }

    [Fact]
    public async Task ExecutarAsync_CredenciaisValidas_DeveRetornarResultadoLogin()
    {
        // Arrange
        var usuario = CriarUsuarioValido();
        _usuarioRepo.ObterPorEmailAsync("joao@email.com").Returns(usuario);
        _tokenService.GerarAccessToken(usuario).Returns("jwt-token-fake");
        _tokenService.GerarRefreshToken().Returns(("raw-refresh-token", new RefreshToken
        {
            TokenHash = RefreshToken.ComputarHash("raw-refresh-token"),
            ExpiraEm = DateTime.UtcNow.AddDays(7)
        }));

        // Act
        var resultado = await _useCase.ExecutarAsync("joao@email.com", "Senh@123", CancellationToken.None);

        // Assert
        resultado.IsError.Should().BeFalse();
        resultado.Value.AccessToken.Should().Be("jwt-token-fake");
        resultado.Value.RefreshToken.Should().Be("raw-refresh-token");
        resultado.Value.Usuario.Should().BeSameAs(usuario);

        // Verifica que o refresh token foi adicionado ao usuário e salvo
        usuario.RefreshTokens.Should().HaveCount(1);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecutarAsync_EmailInexistente_DeveRetornarErroSenhaInvalida()
    {
        // Arrange — nenhum usuário com esse e-mail
        _usuarioRepo.ObterPorEmailAsync("naoexiste@email.com").Returns((Usuario?)null);

        // Act
        var resultado = await _useCase.ExecutarAsync("naoexiste@email.com", "qualquer", CancellationToken.None);

        // Assert — mensagem GENÉRICA (não revela que o e-mail não existe)
        resultado.IsError.Should().BeTrue();
        resultado.FirstError.Code.Should().Be("Usuario.SenhaInvalida");
    }

    [Fact]
    public async Task ExecutarAsync_SenhaIncorreta_DeveRetornarErroSenhaInvalida()
    {
        // Arrange — usuário existe mas senha está errada
        var usuario = CriarUsuarioValido();
        _usuarioRepo.ObterPorEmailAsync("joao@email.com").Returns(usuario);

        // Act
        var resultado = await _useCase.ExecutarAsync("joao@email.com", "SenhaErrada", CancellationToken.None);

        // Assert — MESMA mensagem genérica do cenário de e-mail inexistente
        resultado.IsError.Should().BeTrue();
        resultado.FirstError.Code.Should().Be("Usuario.SenhaInvalida");
    }

    [Fact]
    public async Task ExecutarAsync_UsuarioInativo_DeveRetornarErroInativo()
    {
        // Arrange — senha correta, mas usuário desativado
        var usuario = CriarUsuarioValido();
        usuario.Ativo = false;
        _usuarioRepo.ObterPorEmailAsync("joao@email.com").Returns(usuario);

        // Act
        var resultado = await _useCase.ExecutarAsync("joao@email.com", "Senh@123", CancellationToken.None);

        // Assert
        resultado.IsError.Should().BeTrue();
        resultado.FirstError.Code.Should().Be("Usuario.Inativo");
    }

    [Fact]
    public async Task ExecutarAsync_SenhaErrada_NaoDeveGerarTokens()
    {
        // Arrange
        var usuario = CriarUsuarioValido();
        _usuarioRepo.ObterPorEmailAsync("joao@email.com").Returns(usuario);

        // Act
        await _useCase.ExecutarAsync("joao@email.com", "SenhaErrada", CancellationToken.None);

        // Assert — NÃO deve gerar tokens nem salvar nada
        _tokenService.DidNotReceive().GerarAccessToken(Arg.Any<Usuario>());
        _tokenService.DidNotReceive().GerarRefreshToken();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
```

### 5.2.2 Caso de uso: LoginUseCase.cs

Caso de uso para autenticacao de usuarios. Verifica credenciais, gera tokens
JWT e Refresh Token. A mensagem de erro e propositalmente generica para evitar
enumeracao de e-mails.

**Arquivo: `Agendamentos.Entities/UseCases/Usuarios/LoginUseCase.cs`**

```csharp
// ============================================================================
// Arquivo: UseCases/Usuarios/LoginUseCase.cs
// Descrição: Caso de uso para autenticação (login) de usuários.
//
// CONCEITO IMPORTANTE (Fluxo de Autenticação JWT):
// 1. Cliente envia e-mail + senha
// 2. Servidor verifica credenciais no banco de dados
// 3. Se válidas, gera um Access Token (JWT) + Refresh Token
// 4. Cliente armazena os tokens e os envia em requisições futuras
// 5. Access Token expira → cliente usa Refresh Token para obter novos tokens
//
// CONCEITO IMPORTANTE (Segurança no Login):
// - A mensagem de erro para credenciais inválidas é genérica ("E-mail ou senha
//   inválidos") propositalmente. Se disséssemos "e-mail não encontrado" ou
//   "senha incorreta" separadamente, um atacante poderia enumerar e-mails válidos.
// - Verificamos se o usuário está ativo APÓS validar a senha, pois um usuário
//   desativado não deve saber se sua senha está correta ou não.
//
// CONCEITO IMPORTANTE (Rich Domain Model):
// A verificação da senha é feita pelo método VerificarSenha() da entidade
// Usuario, encapsulando a lógica de comparação BCrypt no domínio.
// ============================================================================

using Agendamentos.Entities.Interfaces;
using Agendamentos.Entities.Models;
using Agendamentos.Entities.UseCases.Common;
using ErrorOr;

namespace Agendamentos.Entities.UseCases.Usuarios;

/// <summary>
/// Caso de uso responsável por autenticar um usuário (login).
///
/// Fluxo de execução:
/// 1. Busca o usuário pelo e-mail
/// 2. Verifica a senha usando o método da entidade (VerificarSenha)
/// 3. Verifica se o usuário está ativo
/// 4. Gera Access Token (JWT) + Refresh Token
/// 5. Salva o Refresh Token no banco de dados
/// 6. Retorna o ResultadoLogin com tokens + dados do usuário
/// </summary>
public class LoginUseCase
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly ITokenService _tokenService;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Construtor com injeção de dependência.
    /// </summary>
    /// <param name="usuarioRepository">Repositório para buscar o usuário pelo e-mail.</param>
    /// <param name="tokenService">Serviço de geração de tokens JWT e Refresh.</param>
    /// <param name="unitOfWork">Unit of Work para persistir o Refresh Token.</param>
    public LoginUseCase(
        IUsuarioRepository usuarioRepository,
        ITokenService tokenService,
        IUnitOfWork unitOfWork)
    {
        _usuarioRepository = usuarioRepository;
        _tokenService = tokenService;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Executa a autenticação do usuário.
    ///
    /// Este caso de uso recebe e-mail e senha diretamente (não uma entidade),
    /// pois o login é uma operação de autenticação que não cria nem modifica
    /// uma entidade — apenas verifica credenciais e gera tokens.
    /// </summary>
    /// <param name="email">E-mail do usuário.</param>
    /// <param name="senha">Senha em texto plano para verificação.</param>
    /// <param name="ct">Token de cancelamento.</param>
    /// <returns>
    /// ErrorOr com ResultadoLogin (tokens + entidade Usuario) em caso de sucesso,
    /// ou Error em caso de falha (credenciais inválidas, usuário inativo).
    /// </returns>
    public async Task<ErrorOr<ResultadoLogin>> ExecutarAsync(
        string email,
        string senha,
        CancellationToken ct)
    {
        // ---------------------------------------------------------------
        // Passo 1: Buscar o usuário pelo e-mail
        // ---------------------------------------------------------------
        // Se o e-mail não existir no banco, retornamos erro genérico.
        // Note que usamos a MESMA mensagem de erro para e-mail inexistente
        // e senha incorreta, evitando enumeração de e-mails por atacantes.
        var usuario = await _usuarioRepository.ObterPorEmailAsync(email);

        if (usuario is null)
            return Erros.Usuario.SenhaInvalida;

        // ---------------------------------------------------------------
        // Passo 2: Verificar a senha usando Rich Domain Model
        // ---------------------------------------------------------------
        // A entidade Usuario encapsula a lógica de verificação BCrypt
        // no método VerificarSenha(). Internamente, ele extrai o salt
        // do hash armazenado, aplica o mesmo algoritmo na senha fornecida
        // e compara os resultados.
        if (!usuario.VerificarSenha(senha))
            return Erros.Usuario.SenhaInvalida;

        // ---------------------------------------------------------------
        // Passo 3: Verificar se o usuário está ativo
        // ---------------------------------------------------------------
        // Um administrador pode desativar um usuário sem excluí-lo (soft delete).
        // Verificamos APÓS validar a senha para não dar pistas sobre o status.
        if (!usuario.Ativo)
            return Erros.Usuario.Inativo;

        // ---------------------------------------------------------------
        // Passo 4: Gerar o Access Token (JWT)
        // ---------------------------------------------------------------
        // O ITokenService.GerarAccessToken cria um JWT contendo as claims:
        // - sub (subject): Id do usuário
        // - email: e-mail do usuário
        // - role: papel do usuário (Cliente, Prestador, Admin)
        var accessToken = _tokenService.GerarAccessToken(usuario);

        // ---------------------------------------------------------------
        // Passo 5: Gerar o Refresh Token
        // ---------------------------------------------------------------
        // O Refresh Token é uma string aleatória criptograficamente segura.
        // Diferente do JWT, ele não contém informações — é apenas um
        // identificador único que referencia o registro no banco de dados.
        //
        // GerarRefreshToken retorna uma tupla com:
        // - tokenBruto: o valor original (será enviado ao cliente via cookie)
        // - entidade: a entidade com o hash SHA256 (será salva no banco)
        //
        // O banco NUNCA armazena o token bruto — apenas o hash. Assim,
        // mesmo que o banco seja comprometido, os tokens não podem ser usados.
        var (tokenBruto, refreshToken) = _tokenService.GerarRefreshToken();
        refreshToken.UsuarioId = usuario.Id;
        usuario.RefreshTokens.Add(refreshToken);

        // ---------------------------------------------------------------
        // Passo 6: Salvar no banco e retornar o resultado de domínio
        // ---------------------------------------------------------------
        await _unitOfWork.SaveChangesAsync(ct);

        return new ResultadoLogin(
            AccessToken: accessToken,
            RefreshToken: tokenBruto,   // Token bruto (para o cookie HttpOnly)
            ExpiraEm: refreshToken.ExpiraEm,
            Usuario: usuario
        );
    }
}
```

## 5.3 Usuários — Refresh Token

### 5.3.1 Teste: RefreshTokenUseCaseTests.cs

Testes para a renovacao de tokens: token valido, token inexistente, token expirado,
token ja revogado e usuario inativo.

**Arquivo: `Agendamentos.Tests/UseCases/Usuarios/RefreshTokenUseCaseTests.cs`**

```csharp
// ============================================================================
// Testes unitários para RefreshTokenUseCase.
//
// CONCEITO IMPORTANTE (Token Rotation):
// O fluxo de refresh é um dos pontos mais críticos de segurança:
// - O token antigo DEVE ser revogado (uso único)
// - O hash do token é comparado, não o token bruto
// - Tokens expirados ou já revogados devem ser rejeitados
// - O usuário deve estar ativo
// ============================================================================

using Agendamentos.Entities.Interfaces;
using Agendamentos.Entities.Models;
using Agendamentos.Entities.Models.Auth;
using Agendamentos.Entities.UseCases.Usuarios;
using FluentAssertions;
using NSubstitute;

namespace Agendamentos.Tests.UseCases.Usuarios;

public class RefreshTokenUseCaseTests
{
    private readonly IUsuarioRepository _usuarioRepo = Substitute.For<IUsuarioRepository>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly RefreshTokenUseCase _useCase;

    // Token bruto de referência para os testes
    private const string TokenBruto = "token-bruto-de-teste";

    public RefreshTokenUseCaseTests()
    {
        _useCase = new RefreshTokenUseCase(_usuarioRepo, _tokenService, _unitOfWork);
    }

    /// <summary>
    /// Cria um usuário com um refresh token ativo (não expirado, não revogado).
    /// </summary>
    private static Usuario CriarUsuarioComTokenAtivo()
    {
        var usuario = new Usuario
        {
            Id = 1,
            Nome = "João",
            Email = "joao@email.com",
            Ativo = true,
            RefreshTokens =
            [
                new RefreshToken
                {
                    Id = 10,
                    TokenHash = RefreshToken.ComputarHash(TokenBruto),
                    ExpiraEm = DateTime.UtcNow.AddDays(7),
                    RevogadoEm = null,
                    UsuarioId = 1
                }
            ]
        };
        return usuario;
    }

    [Fact]
    public async Task ExecutarAsync_TokenValido_DeveRenovarTokensComSucesso()
    {
        // Arrange
        var tokenHash = RefreshToken.ComputarHash(TokenBruto);
        var usuario = CriarUsuarioComTokenAtivo();

        _usuarioRepo.ObterPorRefreshTokenHashAsync(tokenHash).Returns(usuario);
        _tokenService.GerarAccessToken(usuario).Returns("novo-jwt");
        _tokenService.GerarRefreshToken().Returns(("novo-refresh-bruto", new RefreshToken
        {
            TokenHash = RefreshToken.ComputarHash("novo-refresh-bruto"),
            ExpiraEm = DateTime.UtcNow.AddDays(7)
        }));

        // Act
        var resultado = await _useCase.ExecutarAsync(TokenBruto, CancellationToken.None);

        // Assert — sucesso com novos tokens
        resultado.IsError.Should().BeFalse();
        resultado.Value.AccessToken.Should().Be("novo-jwt");
        resultado.Value.RefreshToken.Should().Be("novo-refresh-bruto");

        // O token antigo deve ter sido revogado
        usuario.RefreshTokens[0].Revogado.Should().BeTrue();

        // Novo token foi adicionado ao usuário
        usuario.RefreshTokens.Should().HaveCount(2);

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecutarAsync_TokenInexistente_DeveRetornarErro()
    {
        // Arrange — nenhum usuário possui esse hash
        _usuarioRepo.ObterPorRefreshTokenHashAsync(Arg.Any<string>())
            .Returns((Usuario?)null);

        // Act
        var resultado = await _useCase.ExecutarAsync("token-inexistente", CancellationToken.None);

        // Assert
        resultado.IsError.Should().BeTrue();
        resultado.FirstError.Code.Should().Be("Auth.RefreshTokenInvalido");
    }

    [Fact]
    public async Task ExecutarAsync_TokenExpirado_DeveRetornarErro()
    {
        // Arrange — token existe mas já expirou
        var tokenHash = RefreshToken.ComputarHash(TokenBruto);
        var usuario = new Usuario
        {
            Id = 1, Nome = "João", Email = "joao@email.com", Ativo = true,
            RefreshTokens =
            [
                new RefreshToken
                {
                    TokenHash = tokenHash,
                    ExpiraEm = DateTime.UtcNow.AddMinutes(-1), // EXPIRADO
                    RevogadoEm = null
                }
            ]
        };
        _usuarioRepo.ObterPorRefreshTokenHashAsync(tokenHash).Returns(usuario);

        // Act
        var resultado = await _useCase.ExecutarAsync(TokenBruto, CancellationToken.None);

        // Assert
        resultado.IsError.Should().BeTrue();
        resultado.FirstError.Code.Should().Be("Auth.RefreshTokenInvalido");
    }

    [Fact]
    public async Task ExecutarAsync_TokenJaRevogado_DeveRetornarErro()
    {
        // Arrange — token já foi usado (revogado)
        var tokenHash = RefreshToken.ComputarHash(TokenBruto);
        var usuario = new Usuario
        {
            Id = 1, Nome = "João", Email = "joao@email.com", Ativo = true,
            RefreshTokens =
            [
                new RefreshToken
                {
                    TokenHash = tokenHash,
                    ExpiraEm = DateTime.UtcNow.AddDays(7),
                    RevogadoEm = DateTime.UtcNow.AddHours(-1) // JÁ REVOGADO
                }
            ]
        };
        _usuarioRepo.ObterPorRefreshTokenHashAsync(tokenHash).Returns(usuario);

        // Act
        var resultado = await _useCase.ExecutarAsync(TokenBruto, CancellationToken.None);

        // Assert
        resultado.IsError.Should().BeTrue();
        resultado.FirstError.Code.Should().Be("Auth.RefreshTokenInvalido");
    }

    [Fact]
    public async Task ExecutarAsync_UsuarioInativo_DeveRetornarErro()
    {
        // Arrange — token válido, mas usuário foi desativado
        var tokenHash = RefreshToken.ComputarHash(TokenBruto);
        var usuario = CriarUsuarioComTokenAtivo();
        usuario.Ativo = false; // DESATIVADO

        _usuarioRepo.ObterPorRefreshTokenHashAsync(tokenHash).Returns(usuario);

        // Act
        var resultado = await _useCase.ExecutarAsync(TokenBruto, CancellationToken.None);

        // Assert
        resultado.IsError.Should().BeTrue();
        resultado.FirstError.Code.Should().Be("Usuario.Inativo");
    }
}
```

### 5.3.2 Caso de uso: RefreshTokenUseCase.cs

Caso de uso para renovacao de tokens via Refresh Token. Implementa Token Rotation:
o token antigo e revogado e novos tokens sao gerados.

**Arquivo: `Agendamentos.Entities/UseCases/Usuarios/RefreshTokenUseCase.cs`**

```csharp
// ============================================================================
// Arquivo: UseCases/Usuarios/RefreshTokenUseCase.cs
// Descrição: Caso de uso para renovação de tokens via Refresh Token.
//
// CONCEITO IMPORTANTE (Fluxo de Renovação de Tokens):
// 1. O Access Token (JWT) do cliente expirou
// 2. O cliente envia o Refresh Token para este endpoint
// 3. O servidor busca o Refresh Token no banco de dados
// 4. Verifica se o token é válido (não expirado, não revogado)
// 5. Revoga o token antigo (marca como usado)
// 6. Gera um NOVO Access Token + NOVO Refresh Token
// 7. Retorna os novos tokens ao cliente
//
// CONCEITO IMPORTANTE (Rich Domain Model):
// A revogação do token é feita pelo método Revogar() da entidade
// RefreshToken, que encapsula a lógica de marcar RevogadoEm.
// A verificação de atividade usa a propriedade Ativo da entidade.
// ============================================================================

using Agendamentos.Entities.Interfaces;
using Agendamentos.Entities.Models;
using Agendamentos.Entities.Models.Auth;
using Agendamentos.Entities.UseCases.Common;
using ErrorOr;

namespace Agendamentos.Entities.UseCases.Usuarios;

/// <summary>
/// Caso de uso responsável por renovar tokens usando Refresh Token.
///
/// Fluxo de execução:
/// 1. Busca o usuário que possui o Refresh Token informado
/// 2. Localiza o token específico na lista de tokens do usuário
/// 3. Verifica se o token está ativo (não expirado e não revogado)
/// 4. Revoga o token antigo usando o método da entidade (Revogar)
/// 5. Gera novos tokens (Access Token + Refresh Token)
/// 6. Salva o novo Refresh Token no banco
/// 7. Retorna o ResultadoLogin com tokens atualizados
/// </summary>
public class RefreshTokenUseCase
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly ITokenService _tokenService;
    private readonly IUnitOfWork _unitOfWork;

    public RefreshTokenUseCase(
        IUsuarioRepository usuarioRepository,
        ITokenService tokenService,
        IUnitOfWork unitOfWork)
    {
        _usuarioRepository = usuarioRepository;
        _tokenService = tokenService;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Executa a renovação de tokens usando um Refresh Token válido.
    ///
    /// Este caso de uso recebe o token como string, pois o Refresh Token
    /// não é uma entidade de domínio — é um valor que referencia um registro
    /// no banco de dados.
    /// </summary>
    /// <param name="token">Refresh Token a ser validado e renovado.</param>
    /// <param name="ct">Token de cancelamento.</param>
    /// <returns>
    /// ErrorOr com ResultadoLogin (novos tokens + entidade Usuario) em caso de sucesso,
    /// ou Error se o Refresh Token for inválido/expirado/revogado.
    /// </returns>
    public async Task<ErrorOr<ResultadoLogin>> ExecutarAsync(
        string tokenBruto,
        CancellationToken ct)
    {
        // ---------------------------------------------------------------
        // Passo 1: Computar o hash do token recebido
        // ---------------------------------------------------------------
        // O cliente envia o token bruto (via cookie HttpOnly).
        // O banco armazena apenas o hash SHA256. Para buscar o token,
        // precisamos computar o hash do valor recebido e comparar.
        var tokenHash = RefreshToken.ComputarHash(tokenBruto);

        // ---------------------------------------------------------------
        // Passo 2: Buscar o usuário pelo hash do Refresh Token
        // ---------------------------------------------------------------
        // O repositório busca o usuário que possui o hash informado.
        // Se nenhum usuário for encontrado, o token não existe no banco.
        var usuario = await _usuarioRepository.ObterPorRefreshTokenHashAsync(tokenHash);

        if (usuario is null)
            return Erros.Auth.RefreshTokenInvalido;

        // ---------------------------------------------------------------
        // Passo 3: Localizar o token específico na lista do usuário
        // ---------------------------------------------------------------
        // O usuário pode ter vários refresh tokens (um por dispositivo/sessão).
        // Usamos LINQ Single() pois sabemos que o hash é único.
        var refreshTokenAtual = usuario.RefreshTokens
            .Single(rt => rt.TokenHash == tokenHash);

        // ---------------------------------------------------------------
        // Passo 4: Verificar se o token está ativo (Rich Domain Model)
        // ---------------------------------------------------------------
        // A propriedade Ativo da entidade RefreshToken encapsula a lógica:
        // o token está ativo quando NÃO expirou E NÃO foi revogado.
        if (!refreshTokenAtual.Ativo)
            return Erros.Auth.RefreshTokenInvalido;

        // ---------------------------------------------------------------
        // Passo 5: Verificar se o usuário ainda está ativo
        // ---------------------------------------------------------------
        if (!usuario.Ativo)
            return Erros.Usuario.Inativo;

        // ---------------------------------------------------------------
        // Passo 6: Revogar o token antigo (Rich Domain Model)
        // ---------------------------------------------------------------
        // O método Revogar() da entidade RefreshToken marca RevogadoEm
        // com a data/hora atual. Cada Refresh Token só pode ser usado UMA VEZ.
        refreshTokenAtual.Revogar();

        // ---------------------------------------------------------------
        // Passo 7: Gerar novos tokens
        // ---------------------------------------------------------------
        var novoAccessToken = _tokenService.GerarAccessToken(usuario);
        var (novoTokenBruto, novoRefreshToken) = _tokenService.GerarRefreshToken();
        novoRefreshToken.UsuarioId = usuario.Id;
        usuario.RefreshTokens.Add(novoRefreshToken);

        // ---------------------------------------------------------------
        // Passo 8: Salvar as alterações e retornar o resultado de domínio
        // ---------------------------------------------------------------
        // Duas operações persistidas atomicamente:
        // 1. UPDATE no token antigo (RevogadoEm = agora)
        // 2. INSERT do novo token (com hash)
        await _unitOfWork.SaveChangesAsync(ct);

        return new ResultadoLogin(
            AccessToken: novoAccessToken,
            RefreshToken: novoTokenBruto,   // Token bruto (para o cookie HttpOnly)
            ExpiraEm: novoRefreshToken.ExpiraEm,
            Usuario: usuario
        );
    }
}
```

## 5.4 Usuários — Listar usuários

### 5.4.1 Teste: ListarUsuariosUseCaseTests.cs

Testes para a listagem paginada de usuarios: pagina com mais itens, lista vazia
e ultima pagina.

**Arquivo: `Agendamentos.Tests/UseCases/Usuarios/ListarUsuariosUseCaseTests.cs`**

```csharp
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
```

### 5.4.2 Caso de uso: ListarUsuariosUseCase.cs

Caso de uso para listar usuarios com paginacao por cursor. Um caso de uso simples
que apenas delega ao repositorio e retorna a lista paginada.

**Arquivo: `Agendamentos.Entities/UseCases/Usuarios/ListarUsuariosUseCase.cs`**

```csharp
// ============================================================================
// Arquivo: UseCases/Usuarios/ListarUsuariosUseCase.cs
// Descrição: Caso de uso para listagem de todos os usuários do sistema.
//
// CONCEITO IMPORTANTE (Caso de Uso Simples):
// Nem todo caso de uso precisa de lógica complexa. Este apenas busca dados
// e os retorna. Em Clean Architecture, mesmo operações simples passam pela
// camada de casos de uso para manter a consistência do fluxo.
//
// CONCEITO IMPORTANTE (Retornando Entidades):
// O caso de uso retorna a lista de entidades Usuario diretamente.
// O mapeamento para DTO (omitindo dados sensíveis como SenhaHash)
// é responsabilidade do endpoint na camada API.
// ============================================================================

using Agendamentos.Entities.Common;
using Agendamentos.Entities.Interfaces;
using Agendamentos.Entities.Models;
using ErrorOr;

namespace Agendamentos.Entities.UseCases.Usuarios;

/// <summary>
/// Caso de uso responsável por listar usuários do sistema com paginação.
/// Acessa o repositório de usuários e retorna as entidades paginadas.
/// O endpoint mapeia as entidades para DTOs antes de responder.
/// Restrito a administradores (a autorização é feita no endpoint).
/// </summary>
public class ListarUsuariosUseCase
{
    private readonly IUsuarioRepository _usuarioRepository;

    public ListarUsuariosUseCase(IUsuarioRepository usuarioRepository)
    {
        _usuarioRepository = usuarioRepository;
    }

    /// <summary>
    /// Executa a listagem de usuários com paginação por cursor.
    ///
    /// Fluxo:
    /// 1. Busca limite+1 usuários do repositório (a partir do cursor)
    /// 2. Constrói a ListaPaginada (detecta se há mais itens pelo +1 extra)
    /// 3. Retorna o resultado paginado encapsulado em ErrorOr
    /// </summary>
    /// <param name="cursor">ID do último item da página anterior (null para primeira página).</param>
    /// <param name="limite">Quantidade de itens por página.</param>
    public async Task<ErrorOr<ListaPaginada<Usuario>>> ExecutarAsync(
        int? cursor, int limite)
    {
        var usuarios = await _usuarioRepository.ObterTodosAsync(cursor, limite + 1);
        return ListaPaginada<Usuario>.Criar(usuarios, limite, u => u.Id);
    }
}
```

## 5.5 Prestadores — Criar prestador

### 5.5.1 Teste: CriarPrestadorUseCaseTests.cs

Testes para a criacao de prestador: sucesso, usuario inexistente, usuario ja e
prestador e falha no cache.

**Arquivo: `Agendamentos.Tests/UseCases/Prestadores/CriarPrestadorUseCaseTests.cs`**

```csharp
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
```

### 5.5.2 Caso de uso: CriarPrestadorUseCase.cs

Caso de uso para criacao de prestador. Demonstra operacao atomica com multiplos
repositorios, alteracao de Role e invalidacao de cache.

**Arquivo: `Agendamentos.Entities/UseCases/Prestadores/CriarPrestadorUseCase.cs`**

```csharp
// ============================================================================
// Arquivo: UseCases/Prestadores/CriarPrestadorUseCase.cs
// Descrição: Caso de uso para criação de um novo prestador de serviço.
//
// CONCEITO IMPORTANTE (Use Case com Múltiplas Dependências):
// Este caso de uso demonstra um cenário realista onde uma operação
// de negócio precisa de múltiplos repositórios, Unit of Work e cache.
// Todas as dependências são injetadas via construtor pelo container de DI.
//
// CONCEITO IMPORTANTE (ICachePrestadores — Inversão de Dependência):
// Em vez de depender diretamente de IDistributedCache (pacote de infraestrutura),
// o caso de uso depende de ICachePrestadores (interface do domínio).
// A implementação concreta (CachePrestadores com Redis) fica na Infrastructure.
// Isso mantém o domínio limpo e independente de tecnologias externas.
//
// FLUXO DE NEGÓCIO:
// 1. Verificar se o usuário existe
// 2. Verificar se o usuário já não é prestador
// 3. Configurar a entidade Prestador recebida
// 4. Atualizar a Role do usuário para Prestador
// 5. Salvar tudo atomicamente (Unit of Work)
// 6. Invalidar o cache de prestadores
// 7. Retornar a entidade Prestador criada
// ============================================================================

using Agendamentos.Entities.Enums;
using Agendamentos.Entities.Interfaces;
using Agendamentos.Entities.Models;
using Agendamentos.Entities.UseCases.Common;
using ErrorOr;

namespace Agendamentos.Entities.UseCases.Prestadores;

/// <summary>
/// Caso de uso responsável por criar um novo prestador de serviço.
///
/// Recebe a entidade Prestador do endpoint (com os dados do formulário)
/// e executa todas as validações de negócio antes de persistir.
/// </summary>
public class CriarPrestadorUseCase
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IPrestadorRepository _prestadorRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICachePrestadores _cache;

    public CriarPrestadorUseCase(
        IUsuarioRepository usuarioRepository,
        IPrestadorRepository prestadorRepository,
        IUnitOfWork unitOfWork,
        ICachePrestadores cache)
    {
        _usuarioRepository = usuarioRepository;
        _prestadorRepository = prestadorRepository;
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    /// <summary>
    /// Executa a criação de um novo prestador.
    /// </summary>
    /// <param name="prestador">
    ///     Entidade Prestador com os dados preenchidos pelo endpoint:
    ///     UsuarioId, Especialidade, Descricao, FotoUrl, ValorConsulta,
    ///     DuracaoAtendimentoMinutos.
    /// </param>
    /// <param name="ct">Token de cancelamento.</param>
    /// <returns>
    ///     ErrorOr contendo a entidade Prestador em caso de sucesso,
    ///     ou Error em caso de falha de negócio.
    /// </returns>
    public async Task<ErrorOr<Prestador>> ExecutarAsync(
        Prestador prestador,
        CancellationToken ct)
    {
        // ---------------------------------------------------------------
        // PASSO 1: Verificar se o usuário existe no sistema
        // ---------------------------------------------------------------
        var usuario = await _usuarioRepository.ObterPorIdAsync(prestador.UsuarioId);

        if (usuario is null)
            return Erros.Usuario.NaoEncontrado;

        // ---------------------------------------------------------------
        // PASSO 2: Verificar se o usuário já não é um prestador
        // ---------------------------------------------------------------
        // Um usuário só pode ter um registro de prestador.
        var prestadorExistente = await _prestadorRepository.ObterPorUsuarioIdAsync(prestador.UsuarioId);

        if (prestadorExistente is not null)
            return Erros.Prestador.UsuarioJaEhPrestador;

        // ---------------------------------------------------------------
        // PASSO 3: Configurar valores padrão da entidade
        // ---------------------------------------------------------------
        prestador.Ativo = true;

        // Adicionamos ao repositório (ainda não foi salvo no banco).
        await _prestadorRepository.AdicionarAsync(prestador);

        // ---------------------------------------------------------------
        // PASSO 4: Atualizar a Role do usuário para Prestador
        // ---------------------------------------------------------------
        // Quando um usuário se torna prestador, sua Role muda de Cliente
        // para Prestador. Isso afeta suas permissões no sistema.
        usuario.Role = ERole.Prestador;
        await _usuarioRepository.AtualizarAsync(usuario);

        // ---------------------------------------------------------------
        // PASSO 5: Salvar todas as alterações atomicamente
        // ---------------------------------------------------------------
        // O Unit of Work garante que AMBAS as operações (criar prestador
        // e atualizar role) sejam salvas juntas. Se uma falhar, nenhuma
        // é persistida (transação atômica).
        await _unitOfWork.SaveChangesAsync(ct);

        // ---------------------------------------------------------------
        // PASSO 6: Invalidar o cache de prestadores
        // ---------------------------------------------------------------
        // Quando criamos um novo prestador, a lista cacheada fica desatualizada.
        // A interface ICachePrestadores abstrai os detalhes do Redis.
        // Se o cache estiver indisponível, a operação não deve falhar —
        // o cache será atualizado naturalmente quando expirar.
        try
        {
            await _cache.InvalidarAsync(ct);
        }
        catch
        {
            // Cache indisponível — não impede a criação do prestador.
        }

        // ---------------------------------------------------------------
        // PASSO 7: Retornar a entidade Prestador criada
        // ---------------------------------------------------------------
        // Populamos a navegação para o endpoint ter acesso ao nome/email.
        prestador.Usuario = usuario;
        return prestador;
    }
}
```

### 5.5.3 Teste do Validator: CriarPrestadorValidatorTests.cs

Testes para as regras de validacao do prestador: UsuarioId positivo, especialidade
(minimo 3 caracteres), descricao (minimo 10 caracteres), valor da consulta positivo
e duracao entre 15 e 480 minutos.

**Arquivo: `Agendamentos.Tests/Validators/CriarPrestadorValidatorTests.cs`**

```csharp
// ============================================================================
// Testes unitários para CriarPrestadorValidator (FluentValidation).
// ============================================================================

using Agendamentos.Entities.Models;
using Agendamentos.Entities.UseCases.Prestadores;
using FluentAssertions;

namespace Agendamentos.Tests.Validators;

public class CriarPrestadorValidatorTests
{
    private readonly CriarPrestadorValidator _validator = new();

    private static Prestador CriarPrestadorValido() => new()
    {
        UsuarioId = 1,
        Especialidade = "Dermatologia",
        Descricao = "Especialista em dermatologia clínica e estética",
        ValorConsulta = 200m,
        DuracaoAtendimentoMinutos = 60
    };

    [Fact]
    public async Task Validar_DadosValidos_DevePassarSemErros()
    {
        var prestador = CriarPrestadorValido();

        var resultado = await _validator.ValidateAsync(prestador);

        resultado.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validar_UsuarioIdZero_DeveRetornarErro()
    {
        var prestador = CriarPrestadorValido();
        prestador.UsuarioId = 0;

        var resultado = await _validator.ValidateAsync(prestador);

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == "UsuarioId");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Ab")]
    public async Task Validar_EspecialidadeInvalida_DeveRetornarErro(string? especialidade)
    {
        var prestador = CriarPrestadorValido();
        prestador.Especialidade = especialidade!;

        var resultado = await _validator.ValidateAsync(prestador);

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == "Especialidade");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Curta")]
    public async Task Validar_DescricaoInvalida_DeveRetornarErro(string? descricao)
    {
        var prestador = CriarPrestadorValido();
        prestador.Descricao = descricao!;

        var resultado = await _validator.ValidateAsync(prestador);

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == "Descricao");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Validar_ValorConsultaInvalido_DeveRetornarErro(decimal valor)
    {
        var prestador = CriarPrestadorValido();
        prestador.ValorConsulta = valor;

        var resultado = await _validator.ValidateAsync(prestador);

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == "ValorConsulta");
    }

    [Theory]
    [InlineData(14)]    // Menos que 15 minutos
    [InlineData(481)]   // Mais que 480 minutos (8h)
    public async Task Validar_DuracaoForaDoLimite_DeveRetornarErro(int duracao)
    {
        var prestador = CriarPrestadorValido();
        prestador.DuracaoAtendimentoMinutos = duracao;

        var resultado = await _validator.ValidateAsync(prestador);

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == "DuracaoAtendimentoMinutos");
    }

    [Theory]
    [InlineData(15)]    // Limite inferior (inclusivo)
    [InlineData(480)]   // Limite superior (inclusivo)
    public async Task Validar_DuracaoNosLimites_DevePassar(int duracao)
    {
        var prestador = CriarPrestadorValido();
        prestador.DuracaoAtendimentoMinutos = duracao;

        var resultado = await _validator.ValidateAsync(prestador);

        resultado.IsValid.Should().BeTrue();
    }
}
```

### 5.5.4 Validator: CriarPrestadorValidator.cs

Validador FluentValidation para a entidade Prestador. Define regras de formato
para os campos de entrada.

**Arquivo: `Agendamentos.Entities/UseCases/Prestadores/CriarPrestadorValidator.cs`**

```csharp
// ============================================================================
// Arquivo: UseCases/Prestadores/CriarPrestadorValidator.cs
// Descrição: Validador FluentValidation para a entidade Prestador (criação).
//
// CONCEITO IMPORTANTE (FluentValidation com Entidades):
// Este validador valida diretamente a entidade Prestador. O endpoint cria
// a entidade a partir dos dados da requisição, injeta o IValidator<Prestador>
// e chama ValidateAsync antes de chamar o caso de uso.
//
// Se a validação falhar, o caso de uso NÃO é executado e os erros
// de validação são retornados imediatamente ao cliente.
// ============================================================================

using Agendamentos.Entities.Models;
using FluentValidation;

namespace Agendamentos.Entities.UseCases.Prestadores;

/// <summary>
/// Validador para a entidade <see cref="Prestador"/>.
///
/// Valida os campos de entrada antes de chamar o caso de uso de criação.
/// As regras verificam formato e limites, NÃO regras de negócio.
/// </summary>
public class CriarPrestadorValidator : AbstractValidator<Prestador>
{
    public CriarPrestadorValidator()
    {
        // IDs no banco de dados são sempre positivos.
        RuleFor(x => x.UsuarioId)
            .GreaterThan(0)
            .WithMessage("O ID do usuário deve ser maior que zero.");

        // Especialidade obrigatória com mínimo de 3 caracteres.
        RuleFor(x => x.Especialidade)
            .NotEmpty()
            .WithMessage("A especialidade é obrigatória.")
            .MinimumLength(3)
            .WithMessage("A especialidade deve ter no mínimo 3 caracteres.");

        // Descrição obrigatória com mínimo de 10 caracteres.
        RuleFor(x => x.Descricao)
            .NotEmpty()
            .WithMessage("A descrição é obrigatória.")
            .MinimumLength(10)
            .WithMessage("A descrição deve ter no mínimo 10 caracteres.");

        // Valor da consulta deve ser positivo.
        RuleFor(x => x.ValorConsulta)
            .GreaterThan(0)
            .WithMessage("O valor da consulta deve ser maior que zero.");

        // Duração entre 15 e 480 minutos (8 horas).
        RuleFor(x => x.DuracaoAtendimentoMinutos)
            .InclusiveBetween(15, 480)
            .WithMessage("A duração do atendimento deve ser entre 15 e 480 minutos.");
    }
}
```

## 5.6 Prestadores — Obter prestador

### 5.6.1 Teste: ObterPrestadorUseCaseTests.cs

Testes para a obtencao de um prestador pelo ID: prestador existe e prestador
inexistente.

**Arquivo: `Agendamentos.Tests/UseCases/Prestadores/ObterPrestadorUseCaseTests.cs`**

```csharp
// ============================================================================
// Testes unitários para ObterPrestadorUseCase.
// ============================================================================

using Agendamentos.Entities.Interfaces;
using Agendamentos.Entities.Models;
using Agendamentos.Entities.UseCases.Prestadores;
using FluentAssertions;
using NSubstitute;

namespace Agendamentos.Tests.UseCases.Prestadores;

public class ObterPrestadorUseCaseTests
{
    private readonly IPrestadorRepository _prestadorRepo = Substitute.For<IPrestadorRepository>();
    private readonly ObterPrestadorUseCase _useCase;

    public ObterPrestadorUseCaseTests()
    {
        _useCase = new ObterPrestadorUseCase(_prestadorRepo);
    }

    [Fact]
    public async Task ExecutarAsync_PrestadorExiste_DeveRetornarPrestador()
    {
        // Arrange
        var prestador = new Prestador { Id = 1, Especialidade = "Dermatologia" };
        _prestadorRepo.ObterPorIdAsync(1).Returns(prestador);

        // Act
        var resultado = await _useCase.ExecutarAsync(1, CancellationToken.None);

        // Assert
        resultado.IsError.Should().BeFalse();
        resultado.Value.Should().BeSameAs(prestador);
    }

    [Fact]
    public async Task ExecutarAsync_PrestadorInexistente_DeveRetornarErro()
    {
        // Arrange
        _prestadorRepo.ObterPorIdAsync(999).Returns((Prestador?)null);

        // Act
        var resultado = await _useCase.ExecutarAsync(999, CancellationToken.None);

        // Assert
        resultado.IsError.Should().BeTrue();
        resultado.FirstError.Code.Should().Be("Prestador.NaoEncontrado");
    }
}
```

### 5.6.2 Caso de uso: ObterPrestadorUseCase.cs

Caso de uso para buscar um prestador pelo ID. Diferente do ListarPrestadores,
este não usa cache (consulta por chave primária já é rápida).

**Arquivo: `Agendamentos.Entities/UseCases/Prestadores/ObterPrestadorUseCase.cs`**

```csharp
// ============================================================================
// Arquivo: UseCases/Prestadores/ObterPrestadorUseCase.cs
// Descrição: Caso de uso para obtenção de um prestador específico pelo ID.
//
// CONCEITO IMPORTANTE (Caso de Uso sem Cache):
// Para consultas por ID (que retornam um único registro), o cache
// geralmente não é necessário porque:
// 1. A consulta por chave primária já é muito rápida no banco (índice)
// 2. O volume de requisições por ID específico tende a ser menor
// 3. Gerenciar cache por ID individual aumenta a complexidade
// ============================================================================

using Agendamentos.Entities.Interfaces;
using Agendamentos.Entities.Models;
using Agendamentos.Entities.UseCases.Common;
using ErrorOr;

namespace Agendamentos.Entities.UseCases.Prestadores;

/// <summary>
/// Caso de uso responsável por buscar os dados de um prestador pelo ID.
/// Diferente do ListarPrestadoresUseCase, este NÃO usa cache.
/// Retorna a entidade Prestador — o endpoint mapeia para DTO.
/// </summary>
public class ObterPrestadorUseCase
{
    private readonly IPrestadorRepository _prestadorRepository;

    public ObterPrestadorUseCase(IPrestadorRepository prestadorRepository)
    {
        _prestadorRepository = prestadorRepository;
    }

    /// <summary>
    /// Executa a busca de um prestador pelo ID.
    /// </summary>
    /// <param name="id">ID do prestador a ser consultado.</param>
    /// <param name="ct">Token de cancelamento.</param>
    /// <returns>
    ///     ErrorOr contendo a entidade Prestador em caso de sucesso,
    ///     ou Error.NotFound se o prestador não existir.
    /// </returns>
    public async Task<ErrorOr<Prestador>> ExecutarAsync(
        int id,
        CancellationToken ct)
    {
        var prestador = await _prestadorRepository.ObterPorIdAsync(id);

        if (prestador is null)
            return Erros.Prestador.NaoEncontrado;

        return prestador;
    }
}
```

## 5.7 Prestadores — Listar prestadores

### 5.7.1 Teste: ListarPrestadoresUseCaseTests.cs

Testes para a listagem de prestadores com cache: cache hit (dados no cache),
cache miss (dados no banco + popula cache), cache indisponivel (Redis offline)
e paginacao com cursor.

**Arquivo: `Agendamentos.Tests/UseCases/Prestadores/ListarPrestadoresUseCaseTests.cs`**

```csharp
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
```

### 5.7.2 Caso de uso: ListarPrestadoresUseCase.cs

Caso de uso para listar prestadores ativos COM CACHE (Redis). Implementa
Cache-Aside (Lazy Loading) com paginacao em memoria.

**Arquivo: `Agendamentos.Entities/UseCases/Prestadores/ListarPrestadoresUseCase.cs`**

```csharp
// ============================================================================
// Arquivo: UseCases/Prestadores/ListarPrestadoresUseCase.cs
// Descrição: Caso de uso para listagem de prestadores ativos COM CACHE.
//
// CONCEITO IMPORTANTE (Cache com Redis via ICachePrestadores):
// Cache é uma técnica fundamental para melhorar a performance de APIs.
// A ideia é armazenar dados frequentemente acessados em memória (RAM),
// evitando consultas repetidas ao banco de dados.
//
// ESTRATÉGIA DE CACHE UTILIZADA (Cache-Aside / Lazy Loading):
// 1. O caso de uso PRIMEIRO verifica se os dados estão no cache (cache hit)
// 2. Se SIM → retorna os dados do cache (muito rápido, sem ir ao banco)
// 3. Se NÃO (cache miss) → busca no banco, armazena no cache e retorna
//
// CONCEITO IMPORTANTE (ICachePrestadores — Inversão de Dependência):
// Em vez de depender diretamente de IDistributedCache e fazer serialização
// JSON manualmente, o caso de uso usa ICachePrestadores (interface do domínio).
// A implementação concreta lida com Redis, serialização e TTL internamente.
// ============================================================================

using Agendamentos.Entities.Common;
using Agendamentos.Entities.Interfaces;
using Agendamentos.Entities.Models;
using ErrorOr;

namespace Agendamentos.Entities.UseCases.Prestadores;

/// <summary>
/// Caso de uso responsável por listar prestadores ativos com cache e paginação.
///
/// Implementa Cache-Aside + paginação em memória:
/// 1. Busca a lista completa do cache (ou banco se cache miss)
/// 2. Pagina o resultado em memória usando cursor (keyset)
///
/// CONCEITO IMPORTANTE (Paginação em memória vs no banco):
/// Para prestadores, a paginação é feita EM MEMÓRIA após obter a lista
/// completa do cache. Isso é eficiente porque:
/// - A lista de prestadores é tipicamente pequena (dezenas, não milhares)
/// - O cache Redis já armazena a lista completa (TTL de 1 minuto)
/// - Paginar no banco exigiria cache por página (mais complexo)
///
/// Para entidades com volume maior (Agendamentos, Usuários), a paginação
/// é feita diretamente no banco de dados via keyset pagination.
/// </summary>
public class ListarPrestadoresUseCase
{
    private readonly IPrestadorRepository _prestadorRepository;
    private readonly ICachePrestadores _cache;

    public ListarPrestadoresUseCase(
        IPrestadorRepository prestadorRepository,
        ICachePrestadores cache)
    {
        _prestadorRepository = prestadorRepository;
        _cache = cache;
    }

    /// <summary>
    /// Executa a listagem paginada de prestadores ativos com cache.
    ///
    /// FLUXO COMPLETO:
    /// 1. Tenta buscar a lista completa do cache (rápido, em memória)
    /// 2. Se cache miss → busca no banco e armazena no cache
    /// 3. Aplica paginação por cursor na lista em memória
    /// 4. Retorna o resultado paginado
    /// </summary>
    /// <param name="cursor">ID do último item da página anterior (null para primeira página).</param>
    /// <param name="limite">Quantidade de itens por página.</param>
    /// <param name="ct">Token de cancelamento para operações assíncronas.</param>
    public async Task<ErrorOr<ListaPaginada<Prestador>>> ExecutarAsync(
        int? cursor, int limite, CancellationToken ct)
    {
        // =============================================================
        // PASSO 1: Obter lista completa (cache ou banco)
        // =============================================================
        List<Prestador>? prestadores = null;
        try
        {
            prestadores = await _cache.ObterListaAsync(ct);
        }
        catch
        {
            // Cache indisponível — segue para o banco de dados.
        }

        if (prestadores is null)
        {
            prestadores = await _prestadorRepository.ListarAtivosAsync();

            try
            {
                await _cache.ArmazenarListaAsync(prestadores, ct);
            }
            catch
            {
                // Cache indisponível — retorna dados do banco normalmente.
            }
        }

        // =============================================================
        // PASSO 2: Paginar em memória usando cursor
        // =============================================================
        IEnumerable<Prestador> query = prestadores.OrderBy(p => p.Id);

        if (cursor.HasValue)
            query = query.Where(p => p.Id > cursor.Value);

        var pagina = query.Take(limite + 1).ToList();

        return ListaPaginada<Prestador>.Criar(pagina, limite, p => p.Id);
    }
}
```

## 5.8 Agendamentos — Criar agendamento

### 5.8.1 Teste: CriarAgendamentoUseCaseTests.cs

Testes para a criacao de agendamento: sucesso, prestador inexistente, prestador
inativo, horario fora do expediente e conflito de horario.

**Arquivo: `Agendamentos.Tests/UseCases/Agendamentos/CriarAgendamentoUseCaseTests.cs`**

```csharp
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
```

### 5.8.2 Caso de uso: CriarAgendamentoUseCase.cs

Caso de uso para criacao de agendamento. Implementa 3 etapas de validacao de
negocio: prestador ativo, horario disponivel e sem conflito.

**Arquivo: `Agendamentos.Entities/UseCases/Agendamentos/CriarAgendamentoUseCase.cs`**

```csharp
// ============================================================================
// Arquivo: UseCases/Agendamentos/CriarAgendamentoUseCase.cs
// Descrição: Caso de uso para criação de um novo agendamento.
//
// CONCEITO IMPORTANTE (Use Case com Validações de Negócio):
// Este caso de uso demonstra múltiplas validações de negócio antes
// de permitir a criação de um agendamento:
// 1. O prestador deve existir e estar ativo
// 2. O dia da semana e horário devem estar nos horários disponíveis
// 3. Não pode haver conflito com outro agendamento existente
//
// CONCEITO IMPORTANTE (Rich Domain Model):
// A verificação de horário usa o método Contem() da entidade
// HorarioDisponivel, encapsulando a lógica de "o horário do agendamento
// está dentro do expediente do prestador" na própria entidade.
//
// FLUXO DE VALIDAÇÃO (3 etapas):
// 1. Validator (FluentValidation) → valida formato dos dados (roda antes)
// 2. Caso de uso → valida regras de negócio (prestador ativo? horário ok?)
// 3. Banco de dados → constraints e foreign keys (última linha de defesa)
// ============================================================================

using Agendamentos.Entities.Enums;
using Agendamentos.Entities.Interfaces;
using Agendamentos.Entities.Models;
using Agendamentos.Entities.UseCases.Common;
using ErrorOr;

namespace Agendamentos.Entities.UseCases.Agendamentos;

/// <summary>
/// Caso de uso responsável por criar um novo agendamento.
///
/// Recebe a entidade Agendamento com PrestadorId, ClienteId, DataHora
/// e Observacoes preenchidos pelo endpoint. O caso de uso configura
/// o Status inicial e CriadoEm, além de executar todas as validações.
/// </summary>
public class CriarAgendamentoUseCase
{
    private readonly IAgendamentoRepository _agendamentoRepo;
    private readonly IPrestadorRepository _prestadorRepo;
    private readonly IHorarioDisponivelRepository _horarioRepo;
    private readonly IUnitOfWork _unitOfWork;

    public CriarAgendamentoUseCase(
        IAgendamentoRepository agendamentoRepo,
        IPrestadorRepository prestadorRepo,
        IHorarioDisponivelRepository horarioRepo,
        IUnitOfWork unitOfWork)
    {
        _agendamentoRepo = agendamentoRepo;
        _prestadorRepo = prestadorRepo;
        _horarioRepo = horarioRepo;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Executa a criação de um novo agendamento.
    /// </summary>
    /// <param name="agendamento">
    ///     Entidade Agendamento com PrestadorId, ClienteId, DataHora
    ///     e Observacoes preenchidos. Status e CriadoEm são configurados
    ///     por este caso de uso.
    /// </param>
    /// <param name="ct">Token de cancelamento.</param>
    /// <returns>
    ///     ErrorOr contendo a entidade Agendamento em caso de sucesso,
    ///     ou erros em caso de falha na validação de negócio.
    /// </returns>
    public async Task<ErrorOr<Agendamento>> ExecutarAsync(
        Agendamento agendamento,
        CancellationToken ct)
    {
        // ---------------------------------------------------------------
        // ETAPA 1: Verificar se o prestador existe e está ativo
        // ---------------------------------------------------------------
        var prestador = await _prestadorRepo.ObterPorIdAsync(agendamento.PrestadorId);

        if (prestador is null)
            return Erros.Prestador.NaoEncontrado;

        if (!prestador.Ativo)
            return Erros.Prestador.NaoEncontrado;

        // ---------------------------------------------------------------
        // ETAPA 2: Verificar se o prestador atende neste dia/horário
        // ---------------------------------------------------------------
        // Convertemos a DataHora do agendamento para DayOfWeek + TimeOnly
        // e usamos o método Contem() da entidade HorarioDisponivel
        // para verificar se o horário está dentro do expediente.
        var diaSemana = agendamento.DataHora.DayOfWeek;
        var horaAgendamento = TimeOnly.FromDateTime(agendamento.DataHora);

        var horariosDisponiveis = await _horarioRepo.ListarPorPrestadorIdAsync(agendamento.PrestadorId);

        // Rich Domain Model: usa o método Contem() da entidade
        var horarioValido = horariosDisponiveis.Any(h =>
            h.Contem(diaSemana, horaAgendamento));

        if (!horarioValido)
            return Erros.Agendamento.HorarioIndisponivel;

        // ---------------------------------------------------------------
        // ETAPA 3: Verificar se não há conflito de horário
        // ---------------------------------------------------------------
        // Dois clientes não podem agendar no mesmo horário com o mesmo prestador.
        var existeConflito = await _agendamentoRepo.ExisteConflito(
            agendamento.PrestadorId, agendamento.DataHora);

        if (existeConflito)
            return Erros.Agendamento.ConflitoHorario;

        // ---------------------------------------------------------------
        // ETAPA 4: Configurar valores padrão e criar o agendamento
        // ---------------------------------------------------------------
        agendamento.Status = EStatusAgendamento.Pendente;
        agendamento.CriadoEm = DateTime.UtcNow;

        await _agendamentoRepo.AdicionarAsync(agendamento);
        await _unitOfWork.SaveChangesAsync(ct);

        // ---------------------------------------------------------------
        // ETAPA 5: Buscar o agendamento completo para a resposta
        // ---------------------------------------------------------------
        // Após salvar, buscamos novamente com os dados de navegação
        // (Prestador e Cliente) para que o endpoint possa montar o DTO.
        var agendamentoCriado = await _agendamentoRepo.ObterPorIdAsync(agendamento.Id);

        return agendamentoCriado!;
    }
}
```

### 5.8.3 Teste do Validator: CriarAgendamentoValidatorTests.cs

Testes para as regras de validacao do agendamento: PrestadorId e ClienteId positivos
e DataHora no futuro.

**Arquivo: `Agendamentos.Tests/Validators/CriarAgendamentoValidatorTests.cs`**

```csharp
// ============================================================================
// Testes unitários para CriarAgendamentoValidator (FluentValidation).
// ============================================================================

using Agendamentos.Entities.Models;
using Agendamentos.Entities.UseCases.Agendamentos;
using FluentAssertions;

namespace Agendamentos.Tests.Validators;

public class CriarAgendamentoValidatorTests
{
    private readonly CriarAgendamentoValidator _validator = new();

    [Fact]
    public async Task Validar_DadosValidos_DevePassarSemErros()
    {
        var agendamento = new Agendamento
        {
            PrestadorId = 1,
            ClienteId = 10,
            DataHora = DateTime.UtcNow.AddDays(1) // Futuro
        };

        var resultado = await _validator.ValidateAsync(agendamento);

        resultado.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validar_PrestadorIdZero_DeveRetornarErro()
    {
        var agendamento = new Agendamento
        {
            PrestadorId = 0,
            ClienteId = 10,
            DataHora = DateTime.UtcNow.AddDays(1)
        };

        var resultado = await _validator.ValidateAsync(agendamento);

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == "PrestadorId");
    }

    [Fact]
    public async Task Validar_ClienteIdZero_DeveRetornarErro()
    {
        var agendamento = new Agendamento
        {
            PrestadorId = 1,
            ClienteId = 0,
            DataHora = DateTime.UtcNow.AddDays(1)
        };

        var resultado = await _validator.ValidateAsync(agendamento);

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == "ClienteId");
    }

    [Fact]
    public async Task Validar_DataNoPassado_DeveRetornarErro()
    {
        var agendamento = new Agendamento
        {
            PrestadorId = 1,
            ClienteId = 10,
            DataHora = DateTime.UtcNow.AddHours(-1) // Passado
        };

        var resultado = await _validator.ValidateAsync(agendamento);

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e =>
            e.PropertyName == "DataHora" &&
            e.ErrorMessage.Contains("futuro"));
    }
}
```

### 5.8.4 Validator: CriarAgendamentoValidator.cs

Validador FluentValidation para a entidade Agendamento. Define regras de formato
(IDs positivos, data no futuro). Validacoes de negocio ficam no caso de uso.

**Arquivo: `Agendamentos.Entities/UseCases/Agendamentos/CriarAgendamentoValidator.cs`**

```csharp
// ============================================================================
// Arquivo: UseCases/Agendamentos/CriarAgendamentoValidator.cs
// Descrição: Validador FluentValidation para a entidade Agendamento (criação).
//
// CONCEITO IMPORTANTE (Validação em Camadas):
// Este validador realiza validações "superficiais" (formato, limites):
// - PrestadorId e ClienteId devem ser positivos
// - DataHora deve ser no futuro
//
// Validações de "negócio" (prestador existe? horário disponível? conflito?)
// são feitas no caso de uso, que tem acesso aos repositórios.
// ============================================================================

using Agendamentos.Entities.Models;
using FluentValidation;

namespace Agendamentos.Entities.UseCases.Agendamentos;

/// <summary>
/// Validador para a entidade <see cref="Agendamento"/>.
///
/// Valida os campos de entrada antes de chamar o caso de uso de criação.
/// </summary>
public class CriarAgendamentoValidator : AbstractValidator<Agendamento>
{
    public CriarAgendamentoValidator()
    {
        // O identificador do prestador deve ser positivo.
        RuleFor(a => a.PrestadorId)
            .GreaterThan(0)
            .WithMessage("O prestador deve ser informado.");

        // O identificador do cliente deve ser positivo.
        RuleFor(a => a.ClienteId)
            .GreaterThan(0)
            .WithMessage("O cliente deve ser informado.");

        // A data/hora do agendamento deve ser no futuro.
        RuleFor(a => a.DataHora)
            .GreaterThan(DateTime.UtcNow)
            .WithMessage("A data e hora do agendamento devem ser no futuro.");
    }
}
```

## 5.9 Agendamentos — Cancelar agendamento

### 5.9.1 Teste: CancelarAgendamentoUseCaseTests.cs

Testes para o cancelamento de agendamento: cliente cancelando, prestador cancelando,
agendamento inexistente, usuário sem permissão e status que não permite cancelamento.

**Arquivo: `Agendamentos.Tests/UseCases/Agendamentos/CancelarAgendamentoUseCaseTests.cs`**

```csharp
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
```

### 5.9.2 Caso de uso: CancelarAgendamentoUseCase.cs

Caso de uso para cancelamento de agendamento. Demonstra autorizacao a nivel de
recurso (OWASP BOLA) e transicao de estado via Rich Domain Model.

**Arquivo: `Agendamentos.Entities/UseCases/Agendamentos/CancelarAgendamentoUseCase.cs`**

```csharp
// ============================================================================
// Arquivo: UseCases/Agendamentos/CancelarAgendamentoUseCase.cs
// Descrição: Caso de uso para cancelamento de um agendamento existente.
//
// CONCEITO IMPORTANTE (Rich Domain Model — Transição de Estado):
// Este caso de uso demonstra o uso de entidades enriquecidas:
// - PodeCancelar: propriedade que verifica se o status permite cancelamento
// - PertenceAoUsuario(): método que verifica autorização a nível de recurso
// - Cancelar(): método que executa a transição de estado
//
// As regras de negócio estão encapsuladas na entidade, e o caso de uso
// as orquestra na ordem correta.
//
// CONCEITO IMPORTANTE (Autorização no Caso de Uso):
// A autorização é feita no caso de uso (e não apenas no endpoint) porque
// é uma regra de NEGÓCIO: "somente as partes envolvidas podem cancelar".
// O endpoint verifica se o usuário está autenticado (AuthN),
// mas quem verifica se ele TEM PERMISSÃO para esta ação específica
// é o caso de uso (AuthZ a nível de recurso).
// ============================================================================

using Agendamentos.Entities.Interfaces;
using Agendamentos.Entities.Models;
using Agendamentos.Entities.UseCases.Common;
using ErrorOr;

namespace Agendamentos.Entities.UseCases.Agendamentos;

/// <summary>
/// Caso de uso responsável por cancelar um agendamento existente.
///
/// Usa métodos da entidade Agendamento (Rich Domain Model):
/// - PodeCancelar: verifica se o status permite cancelamento
/// - PertenceAoUsuario(): verifica autorização a nível de recurso
/// - Cancelar(): executa a transição de estado para Cancelado
/// </summary>
public class CancelarAgendamentoUseCase
{
    private readonly IAgendamentoRepository _agendamentoRepo;
    private readonly IUnitOfWork _unitOfWork;

    public CancelarAgendamentoUseCase(
        IAgendamentoRepository agendamentoRepo,
        IUnitOfWork unitOfWork)
    {
        _agendamentoRepo = agendamentoRepo;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Executa o cancelamento de um agendamento.
    /// </summary>
    /// <param name="agendamentoId">ID do agendamento a cancelar.</param>
    /// <param name="usuarioId">ID do usuário que está cancelando (do JWT).</param>
    /// <param name="ct">Token de cancelamento.</param>
    public async Task<ErrorOr<Agendamento>> ExecutarAsync(
        int agendamentoId,
        int usuarioId,
        CancellationToken ct)
    {
        // ---------------------------------------------------------------
        // ETAPA 1: Verificar se o agendamento existe
        // ---------------------------------------------------------------
        var agendamento = await _agendamentoRepo.ObterPorIdAsync(agendamentoId);

        if (agendamento is null)
            return Erros.Agendamento.NaoEncontrado;

        // ---------------------------------------------------------------
        // ETAPA 2: Verificar permissão do usuário (Rich Domain Model)
        // ---------------------------------------------------------------
        // O método PertenceAoUsuario() da entidade verifica se o usuário
        // é o cliente ou o prestador do agendamento.
        if (!agendamento.PertenceAoUsuario(usuarioId, agendamento.Prestador.UsuarioId))
            return Erros.Agendamento.AcessoNegado;

        // ---------------------------------------------------------------
        // ETAPA 3: Verificar se o status permite cancelamento (Rich Domain Model)
        // ---------------------------------------------------------------
        // A propriedade PodeCancelar da entidade encapsula a regra:
        // apenas agendamentos Pendente ou Confirmado podem ser cancelados.
        if (!agendamento.PodeCancelar)
            return Erros.Agendamento.NaoPodeCancelar;

        // ---------------------------------------------------------------
        // ETAPA 4: Cancelar o agendamento (Rich Domain Model)
        // ---------------------------------------------------------------
        // O método Cancelar() da entidade altera o Status para Cancelado.
        agendamento.Cancelar();
        await _agendamentoRepo.AtualizarAsync(agendamento);
        await _unitOfWork.SaveChangesAsync(ct);

        // ---------------------------------------------------------------
        // ETAPA 5: Retornar a entidade atualizada
        // ---------------------------------------------------------------
        return agendamento;
    }
}
```

## 5.10 Agendamentos — Listar agendamentos

### 5.10.1 Teste: ListarAgendamentosUseCaseTests.cs

Testes para a listagem de agendamentos com perspectiva dual: cliente ve seus
agendamentos, prestador ve os dele, prestador inexistente e paginacao com cursor.

**Arquivo: `Agendamentos.Tests/UseCases/Agendamentos/ListarAgendamentosUseCaseTests.cs`**

```csharp
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
```

### 5.10.2 Caso de uso: ListarAgendamentosUseCase.cs

Caso de uso para listar agendamentos com perspectiva dual (cliente ou prestador).
O mesmo caso de uso atende ambos os perfis de usuario com filtros diferentes.

**Arquivo: `Agendamentos.Entities/UseCases/Agendamentos/ListarAgendamentosUseCase.cs`**

```csharp
// ============================================================================
// Arquivo: UseCases/Agendamentos/ListarAgendamentosUseCase.cs
// Descrição: Caso de uso para listagem de agendamentos do usuário logado.
//
// CONCEITO IMPORTANTE (Perspectiva Cliente vs Prestador):
// Este caso de uso implementa uma lógica de "dual perspective":
// - Se o usuário é CLIENTE → busca agendamentos onde ele é o ClienteId
// - Se o usuário é PRESTADOR → primeiro descobre o PrestadorId pelo UsuarioId,
//   depois busca agendamentos onde ele é o PrestadorId
//
// Essa abordagem evita duplicação de código — um único caso de uso atende
// ambos os perfis de usuário com filtros diferentes.
// ============================================================================

using Agendamentos.Entities.Common;
using Agendamentos.Entities.Interfaces;
using Agendamentos.Entities.Models;
using Agendamentos.Entities.UseCases.Common;
using ErrorOr;

namespace Agendamentos.Entities.UseCases.Agendamentos;

/// <summary>
/// Caso de uso responsável por listar agendamentos do usuário logado
/// com paginação por cursor (keyset pagination).
///
/// Retorna uma lista paginada de agendamentos do ponto de vista do usuário:
/// - Clientes veem seus próprios agendamentos
/// - Prestadores veem os agendamentos marcados com eles
/// </summary>
public class ListarAgendamentosUseCase
{
    private readonly IAgendamentoRepository _agendamentoRepo;
    private readonly IPrestadorRepository _prestadorRepo;

    public ListarAgendamentosUseCase(
        IAgendamentoRepository agendamentoRepo,
        IPrestadorRepository prestadorRepo)
    {
        _agendamentoRepo = agendamentoRepo;
        _prestadorRepo = prestadorRepo;
    }

    /// <summary>
    /// Executa a listagem paginada de agendamentos para o usuário informado.
    /// </summary>
    /// <param name="usuarioId">ID do usuário logado (extraído do JWT).</param>
    /// <param name="ehPrestador">Se true, busca pela perspectiva do prestador.</param>
    /// <param name="cursor">ID do último item da página anterior (null para primeira página).</param>
    /// <param name="limite">Quantidade de itens por página.</param>
    /// <param name="ct">Token de cancelamento.</param>
    public async Task<ErrorOr<ListaPaginada<Agendamento>>> ExecutarAsync(
        int usuarioId,
        bool ehPrestador,
        int? cursor,
        int limite,
        CancellationToken ct)
    {
        List<Agendamento> agendamentos;

        if (ehPrestador)
        {
            // CAMINHO DO PRESTADOR: descobre PrestadorId a partir do UsuarioId
            var prestador = await _prestadorRepo.ObterPorUsuarioIdAsync(usuarioId);

            if (prestador is null)
                return Erros.Prestador.NaoEncontrado;

            agendamentos = await _agendamentoRepo
                .ListarPorPrestadorIdAsync(prestador.Id, cursor, limite + 1);
        }
        else
        {
            // CAMINHO DO CLIENTE: filtro direto pelo UsuarioId
            agendamentos = await _agendamentoRepo
                .ListarPorClienteIdAsync(usuarioId, cursor, limite + 1);
        }

        return ListaPaginada<Agendamento>.Criar(agendamentos, limite, a => a.Id);
    }
}
```

## 5.11 Horários — Definir horário

### 5.11.1 Teste: DefinirHorarioUseCaseTests.cs

Testes para a definicao de horario disponivel: prestador existe e prestador
inexistente.

**Arquivo: `Agendamentos.Tests/UseCases/Horarios/DefinirHorarioUseCaseTests.cs`**

```csharp
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
```

### 5.11.2 Caso de uso: DefinirHorarioUseCase.cs

Caso de uso para definir um horario disponivel para um prestador.

**Arquivo: `Agendamentos.Entities/UseCases/Horarios/DefinirHorarioUseCase.cs`**

```csharp
// ============================================================================
// Arquivo: UseCases/Horarios/DefinirHorarioUseCase.cs
// Descrição: Caso de uso para definição de horário disponível de um prestador.
//
// CONCEITO IMPORTANTE (Validação no Caso de Uso vs Endpoint):
// O endpoint é responsável por converter os dados de entrada (ex: strings
// de horário "08:00" → TimeOnly) e criar a entidade HorarioDisponivel.
// O caso de uso é responsável por validar as regras de NEGÓCIO
// (ex: o prestador existe?).
//
// Essa separação garante que cada camada cuide das suas responsabilidades:
// - API: conversão de tipos, validação de formato
// - Use Case: regras de negócio
// ============================================================================

using Agendamentos.Entities.Interfaces;
using Agendamentos.Entities.Models;
using Agendamentos.Entities.UseCases.Common;
using ErrorOr;

namespace Agendamentos.Entities.UseCases.Horarios;

/// <summary>
/// Caso de uso responsável por definir um horário disponível para um prestador.
///
/// Recebe a entidade HorarioDisponivel já com os campos TimeOnly preenchidos
/// (a conversão de string → TimeOnly é feita pelo endpoint).
/// </summary>
public class DefinirHorarioUseCase
{
    private readonly IHorarioDisponivelRepository _horarioRepo;
    private readonly IPrestadorRepository _prestadorRepo;
    private readonly IUnitOfWork _unitOfWork;

    public DefinirHorarioUseCase(
        IHorarioDisponivelRepository horarioRepo,
        IPrestadorRepository prestadorRepo,
        IUnitOfWork unitOfWork)
    {
        _horarioRepo = horarioRepo;
        _prestadorRepo = prestadorRepo;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Executa a definição de um horário disponível.
    /// </summary>
    /// <param name="horario">
    ///     Entidade HorarioDisponivel com PrestadorId, DiaSemana,
    ///     HoraInicio e HoraFim já convertidos para TimeOnly.
    /// </param>
    /// <param name="ct">Token de cancelamento.</param>
    public async Task<ErrorOr<HorarioDisponivel>> ExecutarAsync(
        HorarioDisponivel horario,
        CancellationToken ct)
    {
        // ---------------------------------------------------------------
        // ETAPA 1: Verificar se o prestador existe
        // ---------------------------------------------------------------
        var prestador = await _prestadorRepo.ObterPorIdAsync(horario.PrestadorId);

        if (prestador is null)
            return Erros.Prestador.NaoEncontrado;

        // ---------------------------------------------------------------
        // ETAPA 2: Salvar o horário disponível
        // ---------------------------------------------------------------
        await _horarioRepo.AdicionarAsync(horario);
        await _unitOfWork.SaveChangesAsync(ct);

        // ---------------------------------------------------------------
        // ETAPA 3: Retornar a entidade criada
        // ---------------------------------------------------------------
        return horario;
    }
}
```

### 5.11.3 Teste do Validator: DefinirHorarioValidatorTests.cs

Testes para as regras de validacao do horario disponivel: PrestadorId positivo,
hora de fim posterior a hora de inicio e horas iguais.

**Arquivo: `Agendamentos.Tests/Validators/DefinirHorarioValidatorTests.cs`**

```csharp
// ============================================================================
// Testes unitários para DefinirHorarioValidator (FluentValidation).
// ============================================================================

using Agendamentos.Entities.Models;
using Agendamentos.Entities.UseCases.Horarios;
using FluentAssertions;

namespace Agendamentos.Tests.Validators;

public class DefinirHorarioValidatorTests
{
    private readonly DefinirHorarioValidator _validator = new();

    [Fact]
    public async Task Validar_DadosValidos_DevePassarSemErros()
    {
        var horario = new HorarioDisponivel
        {
            PrestadorId = 1,
            DiaSemana = DayOfWeek.Monday,
            HoraInicio = new TimeOnly(8, 0),
            HoraFim = new TimeOnly(17, 0)
        };

        var resultado = await _validator.ValidateAsync(horario);

        resultado.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validar_PrestadorIdZero_DeveRetornarErro()
    {
        var horario = new HorarioDisponivel
        {
            PrestadorId = 0,
            HoraInicio = new TimeOnly(8, 0),
            HoraFim = new TimeOnly(17, 0)
        };

        var resultado = await _validator.ValidateAsync(horario);

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == "PrestadorId");
    }

    [Fact]
    public async Task Validar_HoraFimAnteriorAHoraInicio_DeveRetornarErro()
    {
        var horario = new HorarioDisponivel
        {
            PrestadorId = 1,
            DiaSemana = DayOfWeek.Monday,
            HoraInicio = new TimeOnly(17, 0),
            HoraFim = new TimeOnly(8, 0) // FIM antes do INÍCIO
        };

        var resultado = await _validator.ValidateAsync(horario);

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e =>
            e.PropertyName == "HoraFim" &&
            e.ErrorMessage.Contains("posterior"));
    }

    [Fact]
    public async Task Validar_HoraFimIgualAHoraInicio_DeveRetornarErro()
    {
        var horario = new HorarioDisponivel
        {
            PrestadorId = 1,
            DiaSemana = DayOfWeek.Monday,
            HoraInicio = new TimeOnly(10, 0),
            HoraFim = new TimeOnly(10, 0) // IGUAIS
        };

        var resultado = await _validator.ValidateAsync(horario);

        resultado.IsValid.Should().BeFalse();
    }
}
```

### 5.11.4 Validator: DefinirHorarioValidator.cs

Validador FluentValidation para a entidade HorarioDisponivel. Verifica se o
PrestadorId e positivo e se a hora de fim e posterior a hora de inicio.

**Arquivo: `Agendamentos.Entities/UseCases/Horarios/DefinirHorarioValidator.cs`**

```csharp
// ============================================================================
// Arquivo: UseCases/Horarios/DefinirHorarioValidator.cs
// Descrição: Validador FluentValidation para a entidade HorarioDisponivel.
//
// CONCEITO IMPORTANTE (Validação de Entidade com TimeOnly):
// Como a entidade HorarioDisponivel usa TimeOnly para HoraInicio e HoraFim,
// a validação de formato de string ("HH:mm") não é mais necessária aqui —
// a conversão de string → TimeOnly é responsabilidade do endpoint (API).
//
// Este validador foca nas regras que podem ser verificadas na entidade:
// - PrestadorId é positivo?
// - A hora de fim é posterior à hora de início?
// ============================================================================

using Agendamentos.Entities.Models;
using FluentValidation;

namespace Agendamentos.Entities.UseCases.Horarios;

/// <summary>
/// Validador para a entidade <see cref="HorarioDisponivel"/>.
///
/// Valida os campos da entidade antes de chamar o caso de uso.
/// A validação de formato de horário (string → TimeOnly) é feita
/// no endpoint, pois a entidade já usa TimeOnly.
/// </summary>
public class DefinirHorarioValidator : AbstractValidator<HorarioDisponivel>
{
    public DefinirHorarioValidator()
    {
        // O identificador do prestador deve ser positivo.
        RuleFor(h => h.PrestadorId)
            .GreaterThan(0)
            .WithMessage("O prestador deve ser informado.");

        // A hora de fim deve ser posterior à hora de início.
        // Como ambos são TimeOnly, a comparação é direta e segura.
        RuleFor(h => h.HoraFim)
            .GreaterThan(h => h.HoraInicio)
            .WithMessage("A hora de fim deve ser posterior à hora de início.");
    }
}
```

## 5.12 Horários — Listar horários

### 5.12.1 Teste: ListarHorariosUseCaseTests.cs

Testes para a listagem de horarios: prestador com horarios e prestador sem
horarios.

**Arquivo: `Agendamentos.Tests/UseCases/Horarios/ListarHorariosUseCaseTests.cs`**

```csharp
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
```

### 5.12.2 Caso de uso: ListarHorariosUseCase.cs

Caso de uso para listar horarios disponiveis de um prestador. Um caso de uso
simples que apenas delega ao repositorio.

**Arquivo: `Agendamentos.Entities/UseCases/Horarios/ListarHorariosUseCase.cs`**

```csharp
// ============================================================================
// Arquivo: UseCases/Horarios/ListarHorariosUseCase.cs
// Descrição: Caso de uso para listagem de horários disponíveis de um prestador.
//
// CONCEITO IMPORTANTE (Caso de Uso Simples):
// Nem todo caso de uso precisa de lógica complexa. Este apenas busca dados
// e os retorna. Em Clean Architecture, mesmo operações simples passam pela
// camada de casos de uso para manter a consistência do fluxo e garantir
// que toda lógica de negócio esteja centralizada.
// ============================================================================

using Agendamentos.Entities.Interfaces;
using Agendamentos.Entities.Models;
using ErrorOr;

namespace Agendamentos.Entities.UseCases.Horarios;

/// <summary>
/// Caso de uso responsável por listar os horários disponíveis de um prestador.
/// Retorna as entidades HorarioDisponivel — o endpoint mapeia para DTO.
/// </summary>
public class ListarHorariosUseCase
{
    private readonly IHorarioDisponivelRepository _horarioRepo;

    public ListarHorariosUseCase(IHorarioDisponivelRepository horarioRepo)
    {
        _horarioRepo = horarioRepo;
    }

    /// <summary>
    /// Executa a listagem de horários disponíveis de um prestador.
    /// </summary>
    /// <param name="prestadorId">ID do prestador cujos horários serão listados.</param>
    /// <param name="ct">Token de cancelamento.</param>
    public async Task<ErrorOr<List<HorarioDisponivel>>> ExecutarAsync(
        int prestadorId,
        CancellationToken ct)
    {
        var horarios = await _horarioRepo.ListarPorPrestadorIdAsync(prestadorId);
        return horarios;
    }
}
```

---

## 5.13 Executar todos os testes

Agora que todos os casos de uso e validators estao implementados, execute todos
os testes para verificar que tudo esta passando:

```bash
dotnet test Agendamentos.Tests --verbosity normal
```

Resultado esperado: **Todos os testes passam (verde).**

Voce pode tambem executar os testes com cobertura:

```bash
dotnet test Agendamentos.Tests --collect:"XPlat Code Coverage"
```


O `--collect:"XPlat Code Coverage"` ativa um **data collector** (um plugin que coleta dados extras durante a execução dos testes). Quando o pacote `coverlet.collector` é instalado, ele se registra no SDK de testes com o friendly name `"XPlat Code Coverage"`. O nome "XPlat" vem de cross-platform, indicando que funciona em Windows, Linux e macOS. Isso o diferencia do collector nativo da Microsoft (`"Code Coverage"`), que funciona apenas no Windows.

O resultado é salvo em um arquivo `coverage.cobertura.xml` dentro da pasta `TestResults/`. Para visualizar de forma mais amigável, instale o `reportgenerator`:

```bash
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coveragereport" -reporttypes:Html
```

Abra `coveragereport/index.html` no navegador para ver o relatório visual.

Antes de rodar os testes novamente, apague a pasta `coveragereport`.

### COMMIT 5 — Testes de Use Cases + Validators + Implementacoes

- Se precisar do meu código para este commit, basta executar no repositório deste curso:

```bash
git checkout commit-5
```

E para voltar para a última versão, basta executar:

```bash
git checkout main
```
