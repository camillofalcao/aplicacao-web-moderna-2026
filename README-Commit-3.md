# 📦 Parte 3 — Camada de Entidades — Interfaces e Definição de Erros

Trabalhamos neste camada com interfaces para permitir a Inversão de Dependência (DIP). As interfaces ficam na camada Entities e as classes concretas ficam nas outras camadas mais externas.

Crie a pasta `Agendamentos.Entities/Interfaces/`.

**Arquivo: `Agendamentos.Entities/Interfaces/IUsuarioRepository.cs`**

```csharp
// ============================================================================
// Arquivo: Interfaces/IUsuarioRepository.cs
// Descrição: Interface que define o contrato do repositório de Usuários.
//
// CONCEITO IMPORTANTE (Repository Pattern):
// O padrão Repository abstrai o acesso a dados, criando uma camada entre
// a lógica de negócio e o banco de dados. Isso permite:
// 1. Trocar o banco de dados sem alterar a lógica de negócio
// 2. Facilitar testes unitários usando mocks
// 3. Centralizar queries relacionadas a uma entidade
//
// A interface fica na camada Entities (domínio) e a implementação
// fica na camada Infrastructure — isso é a Inversão de Dependência (DIP).
// ============================================================================

using Agendamentos.Entities.Models;

namespace Agendamentos.Entities.Interfaces;

/// <summary>
/// Contrato para operações de acesso a dados de Usuários.
/// A implementação concreta fica em Infrastructure/Repositories.
/// </summary>
public interface IUsuarioRepository
{
    /// <summary>
    /// Busca um usuário pelo seu identificador único.
    /// </summary>
    Task<Usuario?> ObterPorIdAsync(long id);

    /// <summary>
    /// Busca um usuário pelo seu endereço de e-mail.
    /// Utilizado no login e na verificação de duplicidade no cadastro.
    /// </summary>
    Task<Usuario?> ObterPorEmailAsync(string email);

    /// <summary>
    /// Adiciona um novo usuário ao banco de dados.
    /// O usuário só será persistido após chamar SaveChangesAsync no UnitOfWork.
    /// </summary>
    Task AdicionarAsync(Usuario usuario);

    /// <summary>
    /// Atualiza os dados de um usuário existente.
    /// </summary>
    Task AtualizarAsync(Usuario usuario);

    /// <summary>
    /// Busca um usuário pelo hash SHA256 de um Refresh Token.
    /// Deve incluir (Include) a coleção de RefreshTokens do usuário.
    /// Utilizado no fluxo de renovação de tokens.
    ///
    /// CONCEITO IMPORTANTE (Busca por hash):
    /// O banco armazena apenas o hash do refresh token (TokenHash).
    /// O caso de uso é responsável por computar o hash do token bruto
    /// recebido do cliente ANTES de chamar este método.
    /// </summary>
    /// <param name="tokenHash">Hash SHA256 do refresh token (Base64).</param>
    Task<Usuario?> ObterPorRefreshTokenHashAsync(string tokenHash);

    /// <summary>
    /// Obtém usuários cadastrados no sistema com paginação por cursor.
    /// Utilizado pelo administrador para listar usuários disponíveis.
    /// </summary>
    /// <param name="cursor">ID do último item da página anterior (null para primeira página).</param>
    /// <param name="quantidade">Quantidade de itens a retornar.</param>
    Task<List<Usuario>> ObterTodosAsync(long? cursor, int quantidade);
}
```

**Arquivo: `Agendamentos.Entities/Interfaces/IPrestadorRepository.cs`**

```csharp
// ============================================================================
// Arquivo: Interfaces/IPrestadorRepository.cs
// Descrição: Interface que define o contrato do repositório de Prestadores.
// ============================================================================

using Agendamentos.Entities.Models;

namespace Agendamentos.Entities.Interfaces;

/// <summary>
/// Contrato para operações de acesso a dados de Prestadores de Serviço.
/// </summary>
public interface IPrestadorRepository
{
    /// <summary>
    /// Busca um prestador pelo seu identificador, incluindo dados do usuário.
    /// </summary>
    Task<Prestador?> ObterPorIdAsync(long id);

    /// <summary>
    /// Busca um prestador pelo ID do usuário associado.
    /// Útil para quando o prestador logado quer ver seus próprios dados.
    /// </summary>
    Task<Prestador?> ObterPorUsuarioIdAsync(long usuarioId);

    /// <summary>
    /// Lista todos os prestadores ativos com dados do usuário.
    /// Usada na página inicial para exibir os cards.
    /// </summary>
    Task<List<Prestador>> ListarAtivosAsync();

    /// <summary>
    /// Adiciona um novo prestador ao banco.
    /// </summary>
    Task AdicionarAsync(Prestador prestador);

    /// <summary>
    /// Atualiza os dados de um prestador existente.
    /// </summary>
    Task AtualizarAsync(Prestador prestador);
}
```

**Arquivo: `Agendamentos.Entities/Interfaces/IHorarioDisponivelRepository.cs`**

```csharp
// ============================================================================
// Arquivo: Interfaces/IHorarioDisponivelRepository.cs
// Descrição: Interface que define o contrato do repositório de Horários.
// ============================================================================

using Agendamentos.Entities.Models;

namespace Agendamentos.Entities.Interfaces;

/// <summary>
/// Contrato para operações de acesso a dados de Horários Disponíveis.
/// </summary>
public interface IHorarioDisponivelRepository
{
    /// <summary>
    /// Lista todos os horários disponíveis de um prestador.
    /// </summary>
    Task<List<HorarioDisponivel>> ListarPorPrestadorIdAsync(long prestadorId);

    /// <summary>
    /// Adiciona um novo horário disponível.
    /// </summary>
    Task AdicionarAsync(HorarioDisponivel horario);

    /// <summary>
    /// Remove um horário disponível.
    /// </summary>
    Task RemoverAsync(HorarioDisponivel horario);

    /// <summary>
    /// Busca um horário específico pelo ID.
    /// </summary>
    Task<HorarioDisponivel?> ObterPorIdAsync(long id);

    /// <summary>
    /// Remove todos os horários disponíveis de um prestador.
    /// Utilizado na substituição completa da agenda semanal —
    /// remove os existentes antes de inserir os novos.
    /// </summary>
    Task RemoverTodosPorPrestadorIdAsync(long prestadorId);
}
```

**Arquivo: `Agendamentos.Entities/Interfaces/IAgendamentoRepository.cs`**

```csharp
// ============================================================================
// Arquivo: Interfaces/IAgendamentoRepository.cs
// Descrição: Interface que define o contrato do repositório de Agendamentos.
// ============================================================================

using Agendamentos.Entities.Models;

namespace Agendamentos.Entities.Interfaces;

/// <summary>
/// Contrato para operações de acesso a dados de Agendamentos.
/// </summary>
public interface IAgendamentoRepository
{
    /// <summary>
    /// Busca um agendamento pelo seu identificador, incluindo prestador e cliente.
    /// </summary>
    Task<Agendamento?> ObterPorIdAsync(long id);

    /// <summary>
    /// Lista agendamentos de um cliente com paginação por cursor.
    /// Ordenados por Id decrescente (mais recentes primeiro).
    /// </summary>
    /// <param name="clienteId">ID do cliente.</param>
    /// <param name="cursor">ID do último item da página anterior (null para primeira página).</param>
    /// <param name="quantidade">Quantidade de itens a retornar.</param>
    Task<List<Agendamento>> ListarPorClienteIdAsync(long clienteId, long? cursor, int quantidade);

    /// <summary>
    /// Lista agendamentos de um prestador com paginação por cursor.
    /// Ordenados por Id decrescente (mais recentes primeiro).
    /// </summary>
    /// <param name="prestadorId">ID do prestador.</param>
    /// <param name="cursor">ID do último item da página anterior (null para primeira página).</param>
    /// <param name="quantidade">Quantidade de itens a retornar.</param>
    Task<List<Agendamento>> ListarPorPrestadorIdAsync(long prestadorId, long? cursor, int quantidade);

    /// <summary>
    /// Verifica se já existe um agendamento no mesmo horário para o prestador.
    /// Evita conflitos de agenda (dois clientes no mesmo horário).
    /// </summary>
    Task<bool> ExisteConflito(long prestadorId, DateTime dataHora, long? agendamentoIdExcluir = null);

    /// <summary>
    /// Adiciona um novo agendamento ao banco.
    /// </summary>
    Task AdicionarAsync(Agendamento agendamento);

    /// <summary>
    /// Atualiza os dados de um agendamento existente.
    /// </summary>
    Task AtualizarAsync(Agendamento agendamento);
}
```

**Arquivo: `Agendamentos.Entities/Interfaces/IUnitOfWork.cs`**

```csharp
// ============================================================================
// Arquivo: Interfaces/IUnitOfWork.cs
// Descrição: Interface que define o contrato do padrão Unit of Work.
//
// CONCEITO IMPORTANTE (Unit of Work):
// O padrão Unit of Work garante que todas as operações de uma transação
// sejam salvas juntas (commit) ou nenhuma seja salva (rollback).
// No EF Core, o DbContext já implementa esse padrão internamente,
// mas criamos esta interface para:
// 1. Manter a abstração na camada de domínio
// 2. Permitir que os handlers chamem SaveChanges de forma explícita
// 3. Facilitar testes unitários
// ============================================================================

namespace Agendamentos.Entities.Interfaces;

/// <summary>
/// Contrato para o padrão Unit of Work.
/// Garante que todas as alterações pendentes sejam salvas atomicamente.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Salva todas as alterações pendentes no banco de dados.
    /// Retorna o número de registros afetados.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

**Arquivo: `Agendamentos.Entities/Interfaces/ITokenService.cs`**

```csharp
// ============================================================================
// Arquivo: Interfaces/ITokenService.cs
// Descrição: Interface que define o contrato para geração de tokens JWT.
//
// CONCEITO IMPORTANTE (JWT - JSON Web Token):
// JWT é um padrão aberto (RFC 7519) para transmissão segura de informações
// entre partes como um objeto JSON. No nosso sistema:
// - O Access Token (JWT) contém claims do usuário (id, email, role)
// - É assinado com uma chave secreta (HMAC SHA256)
// - O servidor valida o token sem consultar o banco de dados
//
// CONCEITO IMPORTANTE (Hash do Refresh Token):
// O refresh token bruto é enviado ao cliente via cookie HttpOnly.
// No banco de dados, armazenamos apenas o hash SHA256 do token.
// Por isso, GerarRefreshToken retorna uma tupla com:
// - TokenBruto: o valor original (para o cookie)
// - Entidade: o RefreshToken com TokenHash preenchido (para o banco)
// ============================================================================

using Agendamentos.Entities.Models;
using Agendamentos.Entities.Models.Auth;

namespace Agendamentos.Entities.Interfaces;

/// <summary>
/// Contrato para o serviço de geração e validação de tokens JWT.
/// A implementação fica na camada Infrastructure.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Gera um Access Token JWT contendo as claims do usuário.
    /// O token tem vida curta (configurável, padrão: 30 minutos).
    /// </summary>
    /// <param name="usuario">Usuário para o qual gerar o token.</param>
    /// <returns>String contendo o JWT codificado.</returns>
    string GerarAccessToken(Usuario usuario);

    /// <summary>
    /// Gera um Refresh Token (string aleatória criptograficamente segura).
    /// O refresh token tem vida longa (configurável, padrão: 7 dias).
    ///
    /// Retorna uma tupla com dois valores:
    /// - TokenBruto: o valor original do token (enviado ao cliente via cookie)
    /// - Entidade: a entidade RefreshToken com o hash SHA256 (salva no banco)
    ///
    /// CONCEITO IMPORTANTE (Tupla nomeada em C#):
    /// Tuplas são úteis quando um método precisa retornar mais de um valor
    /// sem necessidade de criar uma classe ou record específico para isso.
    /// A sintaxe (string TokenBruto, RefreshToken Entidade) define uma
    /// tupla com dois campos nomeados, acessíveis via resultado.TokenBruto
    /// e resultado.Entidade.
    /// </summary>
    /// <returns>
    /// Tupla contendo o token bruto (para o cookie) e a entidade com o hash
    /// (para persistência no banco de dados).
    /// </returns>
    (string TokenBruto, RefreshToken Entidade) GerarRefreshToken();
}
```

**Arquivo: `Agendamentos.Entities/Interfaces/ICachePrestadores.cs`**

```csharp
// ============================================================================
// Arquivo: Interfaces/ICachePrestadores.cs
// Descrição: Interface que abstrai o cache de prestadores ativos.
//
// CONCEITO IMPORTANTE (Inversão de Dependência - DIP):
// Os casos de uso (camada interna) precisam de cache, mas não devem
// depender diretamente de IDistributedCache (pacote de infraestrutura).
// Criamos esta interface na camada Entities (domínio) e a implementação
// concreta fica na camada Infrastructure, que conhece o Redis.
//
// Isso segue o princípio da Inversão de Dependência (DIP):
// - Camada interna define a interface (o QUE precisa)
// - Camada externa implementa (COMO faz)
// - A dependência aponta para dentro, nunca para fora
// ============================================================================

using Agendamentos.Entities.Models;

namespace Agendamentos.Entities.Interfaces;

/// <summary>
/// Contrato para o cache de prestadores ativos.
///
/// Abstrai os detalhes de implementação do cache (Redis, memória, etc.)
/// para que os casos de uso não dependam de pacotes de infraestrutura.
///
/// A implementação concreta (CachePrestadores) fica na camada Infrastructure
/// e utiliza IDistributedCache (Redis) internamente.
/// </summary>
public interface ICachePrestadores
{
    /// <summary>
    /// Tenta obter a lista de prestadores ativos do cache.
    /// Retorna null se não houver dados no cache (cache miss).
    /// </summary>
    /// <param name="ct">Token de cancelamento.</param>
    /// <returns>Lista de prestadores ou null se cache miss.</returns>
    Task<List<Prestador>?> ObterListaAsync(CancellationToken ct = default);

    /// <summary>
    /// Armazena a lista de prestadores ativos no cache com TTL de 1 minuto.
    /// Chamado após buscar os dados do banco de dados (cache-aside).
    /// </summary>
    /// <param name="prestadores">Lista de prestadores a ser cacheada.</param>
    /// <param name="ct">Token de cancelamento.</param>
    Task ArmazenarListaAsync(List<Prestador> prestadores, CancellationToken ct = default);

    /// <summary>
    /// Remove a lista de prestadores do cache (invalidação).
    /// Chamado quando um novo prestador é criado ou atualizado,
    /// forçando a próxima consulta a buscar dados frescos do banco.
    /// </summary>
    /// <param name="ct">Token de cancelamento.</param>
    Task InvalidarAsync(CancellationToken ct = default);
}
```

### 2.5 Definições de Erros

Instale a bilbioteca ErrorOr:

```bash
cd Agendamentos.Entities
dotnet add package ErrorOr
cd ..
```

Crie a pasta `Agendamentos.Entities/UseCases/Common/`.

**Arquivo: `Agendamentos.Entities/UseCases/Common/Erros.cs`**

```csharp
// ============================================================================
// Arquivo: UseCases/Common/Erros.cs
// Descrição: Classe estática que centraliza todos os erros do sistema.
//
// CONCEITO IMPORTANTE (ErrorOr):
// A biblioteca ErrorOr permite retornar erros SEM lançar exceções.
// Em vez de "throw new Exception", retornamos um Error tipado.
// Isso torna o código mais previsível e performático, pois exceções
// são caras em termos de performance e devem ser reservadas para
// situações verdadeiramente excepcionais.
//
// Cada Error tem:
// - Code: código único para identificação programática
// - Description: mensagem legível para o desenvolvedor/usuário
// - Type: tipo do erro (Validation, NotFound, Conflict, Unauthorized, etc.)
// ============================================================================

using ErrorOr;

namespace Agendamentos.Entities.UseCases.Common;

/// <summary>
/// Centraliza todos os erros de domínio do sistema.
/// Organizado por entidade para fácil localização.
///
/// Usar uma classe estática para erros garante:
/// 1. Consistência nas mensagens de erro
/// 2. Reuso em diferentes casos de uso
/// 3. Fácil manutenção e internacionalização futura
/// </summary>
public static class Erros
{
    /// <summary>
    /// Erros relacionados à entidade Usuário.
    /// </summary>
    public static class Usuario
    {
        public static Error EmailJaExiste => Error.Conflict(
            code: "Usuario.EmailJaExiste",
            description: "Já existe um usuário cadastrado com este e-mail.");

        public static Error NaoEncontrado => Error.NotFound(
            code: "Usuario.NaoEncontrado",
            description: "Usuário não encontrado.");

        public static Error SenhaInvalida => Error.Validation(
            code: "Usuario.SenhaInvalida",
            description: "E-mail ou senha inválidos.");

        public static Error Inativo => Error.Forbidden(
            code: "Usuario.Inativo",
            description: "Este usuário está desativado.");
    }

    /// <summary>
    /// Erros relacionados à entidade Prestador.
    /// </summary>
    public static class Prestador
    {
        public static Error NaoEncontrado => Error.NotFound(
            code: "Prestador.NaoEncontrado",
            description: "Prestador não encontrado.");

        public static Error UsuarioJaEhPrestador => Error.Conflict(
            code: "Prestador.UsuarioJaEhPrestador",
            description: "Este usuário já está cadastrado como prestador.");
    }

    /// <summary>
    /// Erros relacionados à entidade Agendamento.
    /// </summary>
    public static class Agendamento
    {
        public static Error NaoEncontrado => Error.NotFound(
            code: "Agendamento.NaoEncontrado",
            description: "Agendamento não encontrado.");

        public static Error ConflitoHorario => Error.Conflict(
            code: "Agendamento.ConflitoHorario",
            description: "Já existe um agendamento neste horário.");

        public static Error HorarioIndisponivel => Error.Validation(
            code: "Agendamento.HorarioIndisponivel",
            description: "O prestador não atende neste dia/horário.");

        public static Error NaoPodeCancelar => Error.Validation(
            code: "Agendamento.NaoPodeCancelar",
            description: "Este agendamento não pode ser cancelado.");

        public static Error AcessoNegado => Error.Forbidden(
            code: "Agendamento.AcessoNegado",
            description: "Você não tem permissão para acessar este agendamento.");
    }

    /// <summary>
    /// Erros relacionados à autenticação e tokens.
    /// </summary>
    public static class Auth
    {
        public static Error RefreshTokenInvalido => Error.Validation(
            code: "Auth.RefreshTokenInvalido",
            description: "Refresh token inválido ou expirado.");

        public static Error NaoAutorizado => Error.Unauthorized(
            code: "Auth.NaoAutorizado",
            description: "Você não está autenticado.");
    }
}
```

### ✅ COMMIT 3 — Interfaces e definições de erros

- Se precisar do meu código para este commit, basta executar no repositório deste curso:

```bash
git checkout commit-3
```

E para voltar para a última versão, basta executar:

```bash
git checkout main
```
