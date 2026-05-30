# 📦 Parte 4 — Projeto de Testes (TDD)

O **TDD (Test-Driven Development)** é uma técnica de desenvolvimento onde os testes são
escritos ANTES do código de produção. O ciclo é:

1. **RED** — Escreva um teste que falha (o código ainda não existe)
2. **GREEN** — Implemente o mínimo necessário para o teste passar
3. **REFACTOR** — Melhore o código mantendo os testes passando

Nesta parte vamos:
1. Criar testes para os Models (que já existem — devem passar imediatamente)
2. Criar testes para os Use Cases (que AINDA NÃO existem — vão falhar)
3. Criar testes para os Validators (que AINDA NÃO existem — vão falhar)
4. Implementar os Use Cases e Validators para fazer TODOS os testes passarem

### 3.1 Criar o projeto de testes

O projeto de testes ja foi criado na Parte 1, mas se por acaso voce estiver
comecando por aqui, execute os comandos abaixo:

```bash
cd Agendamentos
dotnet new xunit -n Agendamentos.Tests --framework net10.0
dotnet sln Agendamentos.slnx add Agendamentos.Tests/Agendamentos.Tests.csproj
dotnet add Agendamentos.Tests/Agendamentos.Tests.csproj reference Agendamentos.Entities/Agendamentos.Entities.csproj
```

### 3.2a Instalar pacotes de teste

```bash
cd Agendamentos.Tests
dotnet add package FluentAssertions --version 8.9.0
dotnet add package NSubstitute --version 5.3.0
dotnet add package coverlet.collector --version 6.0.4
cd ..
```

### 3.2b Instalações adicionais

```bash
cd Agendamentos.Entities
dotnet add package ErrorOr --version 2.1.1
cd ..
```

**Pacotes utilizados:**
- **FluentAssertions** — assertions legíveis: `resultado.Should().BeTrue()`
- **NSubstitute** — mocks (substitutos) para interfaces: `Substitute.For<IRepo>()`
- **coverlet.collector** — coleta metricas de cobertura de codigo
- **ErrorOr** — substitui o lançamento de exceções

---

### 3.3 Testes dos Models (4 arquivos)

Os Models ja existem (foram criados na Parte 1). Portanto, estes testes devem
passar imediatamente. A ideia e validar que a logica encapsulada nas entidades
(Rich Domain Model) esta funcionando corretamente.

Crie a pasta para os testes de Models:

```bash
mkdir -p Agendamentos.Tests/Models
```

#### 3.3.1 UsuarioTests.cs

Testes para a entidade Usuario: hashing de senha com BCrypt, verificacao de senha
e valores padrao.

**Arquivo: `Agendamentos.Tests/Models/UsuarioTests.cs`**

```csharp
using Agendamentos.Entities.Enums;
using Agendamentos.Entities.Models;
using FluentAssertions;

namespace Agendamentos.Tests.Models;

public class UsuarioTests
{
    [Fact]
    public void DefinirSenha_DevePreencherSenhaHash()
    {
        var usuario = new Usuario();
        usuario.DefinirSenha("MinhaSenh@123");
        usuario.SenhaHash.Should().NotBeNullOrWhiteSpace();
        usuario.SenhaHash.Should().StartWith("$2");
    }

    [Fact]
    public void DefinirSenha_NaoDeveArmazenarSenhaEmTextoPlano()
    {
        var senha = "MinhaSenh@123";
        var usuario = new Usuario();
        usuario.DefinirSenha(senha);
        usuario.SenhaHash.Should().NotBe(senha);
    }

    [Fact]
    public void VerificarSenha_SenhaCorreta_DeveRetornarTrue()
    {
        var usuario = new Usuario();
        usuario.DefinirSenha("MinhaSenh@123");
        usuario.VerificarSenha("MinhaSenh@123").Should().BeTrue();
    }

    [Fact]
    public void VerificarSenha_SenhaIncorreta_DeveRetornarFalse()
    {
        var usuario = new Usuario();
        usuario.DefinirSenha("MinhaSenh@123");
        usuario.VerificarSenha("SenhaErrada").Should().BeFalse();
    }

    [Fact]
    public void NovUsuario_DeveTermValoresPadrao()
    {
        var usuario = new Usuario();
        usuario.Role.Should().Be(ERole.Cliente);
        usuario.Ativo.Should().BeTrue();
        usuario.RefreshTokens.Should().BeEmpty();
        usuario.Agendamentos.Should().BeEmpty();
    }
}
```

#### 3.3.2 AgendamentoTests.cs

Testes para a entidade Agendamento: transicao de estado, permissoes e cancelamento.

**Arquivo: `Agendamentos.Tests/Models/AgendamentoTests.cs`**

```csharp
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
```

#### 3.3.3 HorarioDisponivelTests.cs

Testes para o metodo `Contem()` da entidade HorarioDisponivel.

O metodo `Contem()` usa uma comparacao "meio aberta": `hora >= HoraInicio` e `hora < HoraFim`.
Os testes verificam esses limites explicitamente (Boundary Testing):
- HoraInicio e INCLUIDA (`>=`)
- HoraFim e EXCLUIDA (`<`)

Isso e uma decisao de negocio: se o expediente e 08:00-17:00,
um agendamento as 08:00 e aceito, mas as 17:00 nao.

**Arquivo: `Agendamentos.Tests/Models/HorarioDisponivelTests.cs`**

```csharp
// ============================================================================
// Testes unitários para a entidade HorarioDisponivel (Rich Domain Model).
//
// CONCEITO IMPORTANTE (Testes de Limites — Boundary Testing):
// O método Contem() usa uma comparação "meio aberta": hora >= HoraInicio
// e hora < HoraFim. Os testes verificam esses limites explicitamente:
// - HoraInicio é INCLUÍDA (>=)
// - HoraFim é EXCLUÍDA (<)
// Isso é uma decisão de negócio: se o expediente é 08:00-17:00,
// um agendamento às 08:00 é aceito, mas às 17:00 não.
// ============================================================================

using Agendamentos.Entities.Models;
using FluentAssertions;

namespace Agendamentos.Tests.Models;

/// <summary>
/// Testes para o método Contem() da entidade HorarioDisponivel.
/// </summary>
public class HorarioDisponivelTests
{
    /// <summary>
    /// Horário de referência: Segunda-feira, 08:00 às 17:00.
    /// </summary>
    private static HorarioDisponivel CriarHorarioPadrao() => new()
    {
        PrestadorId = 1,
        DiaSemana = DayOfWeek.Monday,
        HoraInicio = new TimeOnly(8, 0),
        HoraFim = new TimeOnly(17, 0)
    };

    [Fact]
    public void Contem_HorarioDentroDoExpediente_DeveRetornarTrue()
    {
        var horario = CriarHorarioPadrao();

        horario.Contem(DayOfWeek.Monday, new TimeOnly(10, 30))
            .Should().BeTrue();
    }

    [Fact]
    public void Contem_ExatamenteNoInicio_DeveRetornarTrue()
    {
        // Boundary: HoraInicio é inclusiva (>=)
        var horario = CriarHorarioPadrao();

        horario.Contem(DayOfWeek.Monday, new TimeOnly(8, 0))
            .Should().BeTrue();
    }

    [Fact]
    public void Contem_ExatamenteNoFim_DeveRetornarFalse()
    {
        // Boundary: HoraFim é exclusiva (<)
        var horario = CriarHorarioPadrao();

        horario.Contem(DayOfWeek.Monday, new TimeOnly(17, 0))
            .Should().BeFalse();
    }

    [Fact]
    public void Contem_AntesDoInicio_DeveRetornarFalse()
    {
        var horario = CriarHorarioPadrao();

        horario.Contem(DayOfWeek.Monday, new TimeOnly(7, 59))
            .Should().BeFalse();
    }

    [Fact]
    public void Contem_DiaDiferente_DeveRetornarFalse()
    {
        // Horário correto, mas dia errado
        var horario = CriarHorarioPadrao();

        horario.Contem(DayOfWeek.Tuesday, new TimeOnly(10, 0))
            .Should().BeFalse();
    }

    [Fact]
    public void Contem_UmMinutoAntesDoFim_DeveRetornarTrue()
    {
        var horario = CriarHorarioPadrao();

        horario.Contem(DayOfWeek.Monday, new TimeOnly(16, 59))
            .Should().BeTrue();
    }
}
```

#### 3.3.4 RefreshTokenTests.cs

Testes para as propriedades derivadas (Expirado, Revogado, Ativo), o metodo Revogar()
e o metodo estatico ComputarHash() da entidade RefreshToken.

**Arquivo: `Agendamentos.Tests/Models/RefreshTokenTests.cs`**

```csharp
// ============================================================================
// Testes unitários para a entidade RefreshToken (Rich Domain Model).
//
// CONCEITO IMPORTANTE (Testes de Propriedades Derivadas):
// As propriedades Expirado, Revogado e Ativo são DERIVADAS de outras
// propriedades (ExpiraEm, RevogadoEm). Os testes verificam que a lógica
// de derivação está correta em cada cenário possível.
// ============================================================================

using Agendamentos.Entities.Models;
using Agendamentos.Entities.Models.Auth;
using FluentAssertions;

namespace Agendamentos.Tests.Models;

/// <summary>
/// Testes para os métodos e propriedades da entidade RefreshToken.
/// Foco: Expirado, Revogado, Ativo, Revogar() e ComputarHash().
/// </summary>
public class RefreshTokenTests
{
    // ========================================================================
    // Expirado — true se DateTime.UtcNow >= ExpiraEm
    // ========================================================================

    [Fact]
    public void Expirado_TokenComDataFutura_DeveRetornarFalse()
    {
        var token = new RefreshToken
        {
            ExpiraEm = DateTime.UtcNow.AddDays(7)
        };

        token.Expirado.Should().BeFalse();
    }

    [Fact]
    public void Expirado_TokenComDataPassada_DeveRetornarTrue()
    {
        var token = new RefreshToken
        {
            ExpiraEm = DateTime.UtcNow.AddMinutes(-1)
        };

        token.Expirado.Should().BeTrue();
    }

    // ========================================================================
    // Revogado — true se RevogadoEm != null
    // ========================================================================

    [Fact]
    public void Revogado_SemRevogacao_DeveRetornarFalse()
    {
        var token = new RefreshToken { RevogadoEm = null };

        token.Revogado.Should().BeFalse();
    }

    [Fact]
    public void Revogado_ComRevogacao_DeveRetornarTrue()
    {
        var token = new RefreshToken { RevogadoEm = DateTime.UtcNow };

        token.Revogado.Should().BeTrue();
    }

    // ========================================================================
    // Ativo — !Expirado && !Revogado
    // ========================================================================

    [Fact]
    public void Ativo_TokenValidoENaoRevogado_DeveRetornarTrue()
    {
        var token = new RefreshToken
        {
            ExpiraEm = DateTime.UtcNow.AddDays(7),
            RevogadoEm = null
        };

        token.Ativo.Should().BeTrue();
    }

    [Fact]
    public void Ativo_TokenExpirado_DeveRetornarFalse()
    {
        var token = new RefreshToken
        {
            ExpiraEm = DateTime.UtcNow.AddMinutes(-1),
            RevogadoEm = null
        };

        token.Ativo.Should().BeFalse();
    }

    [Fact]
    public void Ativo_TokenRevogado_DeveRetornarFalse()
    {
        var token = new RefreshToken
        {
            ExpiraEm = DateTime.UtcNow.AddDays(7),
            RevogadoEm = DateTime.UtcNow
        };

        token.Ativo.Should().BeFalse();
    }

    // ========================================================================
    // Revogar — marca RevogadoEm com DateTime.UtcNow
    // ========================================================================

    [Fact]
    public void Revogar_DevePreencherRevogadoEm()
    {
        // Arrange
        var token = new RefreshToken
        {
            ExpiraEm = DateTime.UtcNow.AddDays(7)
        };

        // Act
        token.Revogar();

        // Assert
        token.RevogadoEm.Should().NotBeNull();
        token.RevogadoEm.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        token.Revogado.Should().BeTrue();
        token.Ativo.Should().BeFalse();
    }

    // ========================================================================
    // ComputarHash — gera hash SHA256 em Base64 a partir do token bruto
    // ========================================================================

    [Fact]
    public void ComputarHash_DeveRetornarHashConsistente()
    {
        // O mesmo input deve sempre gerar o mesmo hash (determinístico)
        var tokenBruto = "meu-token-de-teste-12345";

        var hash1 = RefreshToken.ComputarHash(tokenBruto);
        var hash2 = RefreshToken.ComputarHash(tokenBruto);

        hash1.Should().Be(hash2);
    }

    [Fact]
    public void ComputarHash_DeveRetornarHashDiferenteParaInputsDiferentes()
    {
        var hash1 = RefreshToken.ComputarHash("token-A");
        var hash2 = RefreshToken.ComputarHash("token-B");

        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void ComputarHash_NaoDeveRetornarOTokenOriginal()
    {
        var tokenBruto = "meu-token-secreto";
        var hash = RefreshToken.ComputarHash(tokenBruto);

        hash.Should().NotBe(tokenBruto);
    }

    [Fact]
    public void ComputarHash_DeveRetornarBase64Valido()
    {
        var hash = RefreshToken.ComputarHash("qualquer-token");

        // Base64 válido: pode ser convertido de volta para bytes sem erro
        var act = () => Convert.FromBase64String(hash);
        act.Should().NotThrow();
    }
}
```

### 3.4 Executar os testes de Models

Como os Models ja foram implementados na Parte 1, todos os testes devem passar:

```bash
dotnet test Agendamentos.Tests --filter "FullyQualifiedName~Models" --verbosity normal
```

Resultado esperado: **Todos os testes passam (verde).**

### COMMIT 4 — Testes unitarios para Models

- Se precisar do meu código para este commit, basta executar no repositório deste curso:

```bash
git checkout commit-4
```

E para voltar para a última versão, basta executar:

```bash
git checkout main
```

---
