# 📦 Parte 2 — Camada de Entidades (Domínio)

A camada Entities é o coração do sistema. Aqui ficam os modelos de domínio,
enums, interfaces e casos de uso. Ela **não depende de nenhuma outra camada**.

### 2.1 Enums

Crie a pasta `Agendamentos.Entities/Enums/`.

**Arquivo: `Agendamentos.Entities/Enums/ERole.cs`**

```csharp
// ============================================================================
// Arquivo: Enums/ERole.cs
// Descrição: Enum que define os papéis (roles) dos usuários no sistema.
//            Utilizado para controle de acesso baseado em roles (RBAC).
// ============================================================================

namespace Agendamentos.Entities.Enums;

/// <summary>
/// Representa os diferentes papéis que um usuário pode ter no sistema.
/// Cada role define um nível de permissão diferente.
/// </summary>
public enum ERole
{
    /// <summary>
    /// Usuário comum que pode se cadastrar, fazer login e agendar serviços.
    /// É o papel padrão atribuído no momento do cadastro.
    /// </summary>
    Cliente = 0,

    /// <summary>
    /// Prestador de serviços que possui uma agenda própria.
    /// Só pode ser criado pelo Administrador.
    /// Pode gerenciar seus horários disponíveis e visualizar seus agendamentos.
    /// </summary>
    Prestador = 1,

    /// <summary>
    /// Administrador do sistema com acesso total.
    /// Pode criar prestadores de serviços e gerenciar todos os recursos.
    /// Um usuário admin é criado automaticamente no seed do banco de dados.
    /// </summary>
    Admin = 2
}
```

**Arquivo: `Agendamentos.Entities/Enums/EStatusAgendamento.cs`**

```csharp
// ============================================================================
// Arquivo: Enums/EStatusAgendamento.cs
// Descrição: Enum que define os possíveis estados de um agendamento.
//            Representa o ciclo de vida de um agendamento no sistema.
// ============================================================================

namespace Agendamentos.Entities.Enums;

/// <summary>
/// Representa os possíveis estados de um agendamento.
/// O fluxo típico é: Pendente → Confirmado → Concluido
/// O agendamento pode ser cancelado a qualquer momento antes de ser concluído.
/// </summary>
public enum EStatusAgendamento
{
    /// <summary>
    /// Agendamento recém-criado, aguardando confirmação do prestador.
    /// </summary>
    Pendente = 0,

    /// <summary>
    /// Agendamento confirmado pelo prestador de serviços.
    /// </summary>
    Confirmado = 1,

    /// <summary>
    /// Agendamento cancelado pelo cliente ou pelo prestador.
    /// </summary>
    Cancelado = 2,

    /// <summary>
    /// Serviço realizado com sucesso. Estado final do fluxo normal.
    /// </summary>
    Concluido = 3
}
```

### 2.2 Models (Rich Domain Model)

Adicione o pacote BCrypt:

```bash
cd Agendamentos.Entities
dotnet add package BCrypt.Net-Next
cd ..
```

Crie a pasta `Agendamentos.Entities/Models/`.

**Arquivo: `Agendamentos.Entities/Models/Usuario.cs`**

```csharp
// ============================================================================
// Arquivo: Models/Usuario.cs
// Descrição: Entidade que representa um usuário do sistema.
//            Contém informações de autenticação e perfil.
//            Relaciona-se com Prestador (1:1 opcional) e Agendamento (1:N).
// ============================================================================

using Agendamentos.Entities.Enums;
using Agendamentos.Entities.Models.Auth;

namespace Agendamentos.Entities.Models;

/// <summary>
/// Entidade de domínio que representa um usuário do sistema.
///
/// CONCEITO IMPORTANTE (Clean Architecture):
/// As entidades ficam na camada mais interna da arquitetura e não dependem
/// de nenhuma outra camada. Elas representam as regras de negócio fundamentais.
///
/// Note que esta classe NÃO possui atributos do Entity Framework (como [Key], [Required]).
/// A configuração do banco é feita na camada Infrastructure via Fluent API,
/// mantendo as entidades "limpas" e independentes de framework.
/// </summary>
public class Usuario
{
    /// <summary>
    /// Identificador único do usuário (gerado via SnowflakeID).
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Nome completo do usuário.
    /// </summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// Endereço de e-mail do usuário. Deve ser único no sistema.
    /// Utilizado como credencial de login junto com a senha.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Hash da senha do usuário.
    /// NUNCA armazenamos senhas em texto plano! Utilizamos BCrypt para gerar o hash.
    /// O BCrypt já inclui o salt no próprio hash, facilitando a verificação.
    /// </summary>
    public string SenhaHash { get; set; } = string.Empty;

    /// <summary>
    /// Papel/perfil do usuário no sistema (Cliente, Prestador ou Admin).
    /// Determina quais endpoints e funcionalidades o usuário pode acessar.
    /// </summary>
    public ERole Role { get; set; } = ERole.Cliente;

    /// <summary>
    /// Indica se o usuário está ativo no sistema.
    /// Permite "desativar" um usuário sem excluí-lo (soft delete).
    /// </summary>
    public bool Ativo { get; set; } = true;

    /// <summary>
    /// Data e hora de criação do registro.
    /// </summary>
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    // ---- Comportamento (Rich Domain Model) ----
    // Em vez de deixar a entidade "anêmica" (só dados), adicionamos
    // métodos que encapsulam regras de negócio da própria entidade.

    /// <summary>
    /// Define a senha do usuário, gerando o hash BCrypt automaticamente.
    /// O BCrypt inclui um salt aleatório e é projetado para ser lento,
    /// dificultando ataques de força bruta.
    /// </summary>
    /// <param name="senha">Senha em texto plano informada pelo usuário.</param>
    public void DefinirSenha(string senha)
        => SenhaHash = BCrypt.Net.BCrypt.HashPassword(senha);

    /// <summary>
    /// Verifica se a senha informada corresponde ao hash armazenado.
    /// Usado no login para autenticar o usuário.
    /// </summary>
    /// <param name="senha">Senha em texto plano a ser verificada.</param>
    /// <returns>true se a senha corresponde ao hash, false caso contrário.</returns>
    public bool VerificarSenha(string senha)
        => BCrypt.Net.BCrypt.Verify(senha, SenhaHash);

    // ---- Propriedades de Navegação (Navigation Properties) ----
    // O EF Core usa estas propriedades para representar relacionamentos entre tabelas.

    /// <summary>
    /// Dados do prestador de serviço (caso o usuário seja um Prestador).
    /// Relacionamento 1:1 opcional — só existe se Role == Prestador.
    /// </summary>
    public Prestador? Prestador { get; set; }

    /// <summary>
    /// Lista de agendamentos feitos por este usuário (como cliente).
    /// Relacionamento 1:N — um cliente pode ter vários agendamentos.
    /// </summary>
    public List<Agendamento> Agendamentos { get; set; } = [];

    /// <summary>
    /// Tokens de refresh associados a este usuário.
    /// Cada dispositivo/sessão pode ter seu próprio refresh token.
    /// </summary>
    public List<RefreshToken> RefreshTokens { get; set; } = [];
}
```

**Arquivo: `Agendamentos.Entities/Models/Prestador.cs`**

```csharp
// ============================================================================
// Arquivo: Models/Prestador.cs
// Descrição: Entidade que representa um prestador de serviços.
//            Contém informações profissionais e está vinculado a um Usuário.
//            Possui horários disponíveis e recebe agendamentos de clientes.
// ============================================================================

namespace Agendamentos.Entities.Models;

/// <summary>
/// Entidade de domínio que representa um prestador de serviços.
///
/// Um prestador é sempre vinculado a um Usuário com Role = Prestador.
/// O administrador cria o prestador, que recebe uma agenda própria
/// onde define seus horários disponíveis para atendimento.
///
/// Os dados do prestador são exibidos na página inicial em formato de cards,
/// e essa listagem é cacheada com Redis para melhor performance.
/// </summary>
public class Prestador
{
    /// <summary>
    /// Identificador único do prestador (gerado via SnowflakeID).
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Chave estrangeira para o Usuário associado a este prestador.
    /// Cada prestador está vinculado a exatamente um usuário.
    /// </summary>
    public long UsuarioId { get; set; }

    /// <summary>
    /// Especialidade ou área de atuação do prestador.
    /// Exemplos: "Cabeleireiro", "Dentista", "Personal Trainer", "Advogado".
    /// </summary>
    public string Especialidade { get; set; } = string.Empty;

    /// <summary>
    /// Descrição detalhada dos serviços oferecidos pelo prestador.
    /// Exibida no card do prestador na página inicial.
    /// </summary>
    public string Descricao { get; set; } = string.Empty;

    /// <summary>
    /// URL da foto de perfil do prestador.
    /// Pode ser uma URL externa ou um caminho relativo para imagem local.
    /// </summary>
    public string? FotoUrl { get; set; }

    /// <summary>
    /// Valor cobrado por sessão/consulta em reais (R$).
    /// </summary>
    public decimal ValorConsulta { get; set; }

    /// <summary>
    /// Duração padrão de cada atendimento em minutos.
    /// Usado para calcular os slots disponíveis na agenda.
    /// </summary>
    public int DuracaoAtendimentoMinutos { get; set; } = 60;

    /// <summary>
    /// Indica se o prestador está ativo e aceitando novos agendamentos.
    /// </summary>
    public bool Ativo { get; set; } = true;

    // ---- Propriedades de Navegação ----

    /// <summary>
    /// Usuário vinculado a este prestador.
    /// Através dele acessamos Nome e Email do prestador.
    /// </summary>
    public Usuario Usuario { get; set; } = null!;

    /// <summary>
    /// Horários disponíveis para atendimento definidos pelo prestador.
    /// Cada registro define um dia da semana com hora de início e fim.
    /// </summary>
    public List<HorarioDisponivel> HorariosDisponiveis { get; set; } = [];

    /// <summary>
    /// Agendamentos marcados com este prestador.
    /// </summary>
    public List<Agendamento> Agendamentos { get; set; } = [];
}
```

**Arquivo: `Agendamentos.Entities/Models/HorarioDisponivel.cs`**

```csharp
// ============================================================================
// Arquivo: Models/HorarioDisponivel.cs
// Descrição: Entidade que define os horários disponíveis de um prestador.
//            Cada registro indica um dia da semana com hora de início e fim.
//            O sistema usa esses dados para calcular os slots de agendamento.
// ============================================================================

namespace Agendamentos.Entities.Models;

/// <summary>
/// Entidade de domínio que representa um horário disponível do prestador.
///
/// Exemplo: Um dentista que atende segunda a sexta das 08:00 às 17:00
/// teria 5 registros de HorarioDisponivel, um para cada dia da semana.
///
/// O sistema usa DayOfWeek (enum nativo do .NET) para representar os dias,
/// e TimeOnly para representar horários (sem componente de data).
/// </summary>
public class HorarioDisponivel
{
    /// <summary>
    /// Identificador único do horário disponível (gerado via SnowflakeID).
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Chave estrangeira para o Prestador dono deste horário.
    /// </summary>
    public long PrestadorId { get; set; }

    /// <summary>
    /// Dia da semana em que o prestador está disponível.
    /// Usa o enum DayOfWeek do .NET: Sunday=0, Monday=1, ..., Saturday=6.
    /// </summary>
    public DayOfWeek DiaSemana { get; set; }

    /// <summary>
    /// Hora de início do expediente neste dia da semana.
    /// Exemplo: 08:00
    /// </summary>
    public TimeOnly HoraInicio { get; set; }

    /// <summary>
    /// Hora de fim do expediente neste dia da semana.
    /// Exemplo: 17:00
    /// </summary>
    public TimeOnly HoraFim { get; set; }

    // ---- Comportamento (Rich Domain Model) ----

    /// <summary>
    /// Verifica se este horário cobre o dia da semana e a hora informados.
    /// Usado para validar se um agendamento está dentro do expediente.
    ///
    /// Exemplo: Se HoraInicio=08:00 e HoraFim=17:00, DiaSemana=Monday:
    ///   Contem(Monday, 10:00) → true  (dentro do expediente)
    ///   Contem(Monday, 17:00) → false (limite superior exclusivo)
    ///   Contem(Tuesday, 10:00) → false (dia diferente)
    /// </summary>
    public bool Contem(DayOfWeek dia, TimeOnly hora)
        => DiaSemana == dia && hora >= HoraInicio && hora < HoraFim;

    // ---- Propriedade de Navegação ----

    /// <summary>
    /// Prestador ao qual este horário pertence.
    /// </summary>
    public Prestador Prestador { get; set; } = null!;
}
```

**Arquivo: `Agendamentos.Entities/Models/Agendamento.cs`**

```csharp
// ============================================================================
// Arquivo: Models/Agendamento.cs
// Descrição: Entidade que representa um agendamento entre cliente e prestador.
//            Contém data/hora, status e observações sobre o serviço agendado.
// ============================================================================

using Agendamentos.Entities.Enums;

namespace Agendamentos.Entities.Models;

/// <summary>
/// Entidade de domínio que representa um agendamento de serviço.
///
/// Um agendamento vincula um Cliente a um Prestador em uma data/hora específica.
/// Possui um ciclo de vida representado pelo EStatusAgendamento:
///   Pendente → Confirmado → Concluido (fluxo normal)
///   Pendente/Confirmado → Cancelado (cancelamento)
/// </summary>
public class Agendamento
{
    /// <summary>
    /// Identificador único do agendamento (gerado via SnowflakeID).
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Chave estrangeira para o Prestador de serviço.
    /// </summary>
    public long PrestadorId { get; set; }

    /// <summary>
    /// Chave estrangeira para o Cliente (Usuário) que agendou.
    /// </summary>
    public long ClienteId { get; set; }

    /// <summary>
    /// Data e hora marcada para o atendimento.
    /// Armazenada em UTC para evitar problemas com fuso horário.
    /// </summary>
    public DateTime DataHora { get; set; }

    /// <summary>
    /// Status atual do agendamento (Pendente, Confirmado, Cancelado, Concluido).
    /// </summary>
    public EStatusAgendamento Status { get; set; } = EStatusAgendamento.Pendente;

    /// <summary>
    /// Observações adicionais feitas pelo cliente ao agendar.
    /// Exemplo: "Preciso de corte e barba" ou "Consulta de retorno".
    /// </summary>
    public string? Observacoes { get; set; }

    /// <summary>
    /// Data e hora em que o agendamento foi criado.
    /// </summary>
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    // ---- Comportamento (Rich Domain Model) ----

    /// <summary>
    /// Indica se o agendamento pode ser cancelado.
    /// Apenas agendamentos nos status Pendente ou Confirmado podem ser cancelados.
    /// Cancelado e Concluído são estados finais e imutáveis.
    /// </summary>
    public bool PodeCancelar => Status is EStatusAgendamento.Pendente
                                or EStatusAgendamento.Confirmado;

    /// <summary>
    /// Verifica se o agendamento pertence ao usuário informado,
    /// seja como cliente (ClienteId) ou como prestador (UsuarioId do prestador).
    /// Usado para autorização a nível de recurso.
    /// </summary>
    /// <param name="usuarioId">ID do usuário logado.</param>
    /// <param name="prestadorUsuarioId">ID do usuário vinculado ao prestador.</param>
    public bool PertenceAoUsuario(long usuarioId, long prestadorUsuarioId)
        => ClienteId == usuarioId || prestadorUsuarioId == usuarioId;

    /// <summary>
    /// Cancela o agendamento, alterando seu status para Cancelado.
    /// Deve-se verificar PodeCancelar antes de chamar este método.
    /// </summary>
    public void Cancelar() => Status = EStatusAgendamento.Cancelado;

    // ---- Propriedades de Navegação ----

    /// <summary>
    /// Prestador de serviço associado a este agendamento.
    /// </summary>
    public Prestador Prestador { get; set; } = null!;

    /// <summary>
    /// Cliente (Usuário) que criou este agendamento.
    /// </summary>
    public Usuario Cliente { get; set; } = null!;
}
```

**Arquivo: `Agendamentos.Entities/Models/Auth/RefreshToken.cs`**

```csharp
// ============================================================================
// Arquivo: Models/Auth/RefreshToken.cs
// Descrição: Entidade que representa um Refresh Token para autenticação JWT.
//
// CONCEITO IMPORTANTE (JWT + Refresh Token):
// O JWT (Access Token) tem vida curta (ex: 30 minutos) por segurança.
// Quando expira, o cliente usa o Refresh Token (vida longa) para obter
// um novo JWT sem precisar refazer o login. Isso melhora a experiência
// do usuário mantendo a segurança.
//
// CONCEITO IMPORTANTE (Armazenar apenas o HASH do Refresh Token):
// O banco de dados NÃO armazena o valor bruto do refresh token — apenas
// o hash SHA256 dele. Isso é análogo a armazenar o hash da senha em vez
// da senha em si. Se o banco for comprometido, o atacante não consegue
// usar os hashes para se autenticar, pois SHA256 é uma função de mão
// única (one-way function).
//
// Fluxo:
// 1. O servidor gera um token aleatório de 64 bytes (Base64)
// 2. Computa o SHA256 desse token → armazena o hash no banco
// 3. Envia o token bruto ao cliente via cookie HttpOnly
// 4. Quando o cliente envia o token de volta, o servidor computa o
//    hash novamente e compara com o hash armazenado no banco
// ============================================================================

using System.Security.Cryptography;
using System.Text;

namespace Agendamentos.Entities.Models.Auth;

/// <summary>
/// Entidade de domínio que representa um Refresh Token.
///
/// Fluxo de autenticação com Refresh Token:
/// 1. Usuário faz login → recebe Access Token (JWT) + Refresh Token em cookies HttpOnly
/// 2. Access Token expira → navegador envia Refresh Token automaticamente via cookie
/// 3. Servidor computa o hash do token recebido e busca no banco
/// 4. Se válido, gera novo Access Token + novo Refresh Token
/// 5. Refresh Token antigo é revogado (usado uma única vez)
///
/// Este padrão é chamado de "Rotating Refresh Tokens" e é mais seguro,
/// pois cada refresh token só pode ser usado uma vez.
/// </summary>
public class RefreshToken
{
    /// <summary>
    /// Identificador único do refresh token (gerado via SnowflakeID).
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Hash SHA256 do valor do token (o valor bruto NUNCA é armazenado no banco).
    ///
    /// CONCEITO IMPORTANTE (Por que armazenar o hash e não o token bruto?):
    /// Se um atacante obtiver acesso ao banco de dados (SQL injection, backup
    /// vazado, etc.), ele verá apenas hashes — que são inúteis para autenticação.
    /// O token bruto é enviado ao cliente via cookie HttpOnly e nunca é persistido
    /// no servidor.
    ///
    /// Usamos SHA256 (e não BCrypt como nas senhas) porque:
    /// - Refresh tokens são strings aleatórias de 64 bytes (alta entropia)
    /// - Não são vulneráveis a ataques de dicionário ou força bruta
    /// - SHA256 é rápido, o que é desejável para validação em cada requisição
    /// - BCrypt seria desnecessariamente lento para tokens de alta entropia
    /// </summary>
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>
    /// Chave estrangeira para o Usuário dono deste token.
    /// </summary>
    public long UsuarioId { get; set; }

    /// <summary>
    /// Data e hora em que o token expira.
    /// Após essa data, o token não pode mais ser utilizado.
    /// </summary>
    public DateTime ExpiraEm { get; set; }

    /// <summary>
    /// Data e hora em que o token foi criado.
    /// </summary>
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Data e hora em que o token foi revogado (utilizado ou invalidado).
    /// Se null, o token ainda não foi utilizado.
    /// </summary>
    public DateTime? RevogadoEm { get; set; }

    /// <summary>
    /// Verifica se o token já expirou (data atual > data de expiração).
    /// </summary>
    public bool Expirado => DateTime.UtcNow >= ExpiraEm;

    /// <summary>
    /// Verifica se o token foi revogado (já foi utilizado ou invalidado).
    /// </summary>
    public bool Revogado => RevogadoEm != null;

    /// <summary>
    /// Verifica se o token está ativo (não expirou E não foi revogado).
    /// Apenas tokens ativos podem ser usados para gerar novos access tokens.
    /// </summary>
    public bool Ativo => !Expirado && !Revogado;

    /// <summary>
    /// Revoga este token, marcando a data/hora de revogação.
    /// Um token revogado não pode mais ser utilizado para renovação.
    /// </summary>
    public void Revogar() => RevogadoEm = DateTime.UtcNow;

    // ---- Método estático para computar hash ----

    /// <summary>
    /// Computa o hash SHA256 de um token bruto.
    ///
    /// Este método é usado em dois momentos:
    /// 1. Na GERAÇÃO: para calcular o hash antes de salvar no banco
    /// 2. Na VALIDAÇÃO: para calcular o hash do token recebido do cliente
    ///    e comparar com o hash armazenado no banco
    ///
    /// CONCEITO IMPORTANTE (SHA256):
    /// SHA256 é uma função hash criptográfica que transforma qualquer
    /// entrada em uma string de 256 bits (32 bytes). Propriedades:
    /// - Determinística: mesma entrada → mesmo hash (sempre)
    /// - Irreversível: impossível obter o token original a partir do hash
    /// - Resistente a colisões: extremamente difícil encontrar duas
    ///   entradas diferentes que gerem o mesmo hash
    /// </summary>
    /// <param name="tokenBruto">O valor bruto do refresh token (Base64).</param>
    /// <returns>Hash SHA256 do token, codificado em Base64.</returns>
    public static string ComputarHash(string tokenBruto)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(tokenBruto));
        return Convert.ToBase64String(bytes);
    }

    // ---- Propriedade de Navegação ----

    /// <summary>
    /// Usuário ao qual este refresh token pertence.
    /// </summary>
    public Usuario Usuario { get; set; } = null!;
}
```

**Arquivo: `Agendamentos.Entities/Models/Auth/ResultadoLogin.cs`**

```csharp
// ============================================================================
// Arquivo: Models/Auth/ResultadoLogin.cs
// Descrição: Tipo de domínio que representa o resultado de um login ou
//            renovação de token bem-sucedido.
//
// CONCEITO IMPORTANTE (Tipo de Resultado de Domínio):
// Este record encapsula todos os dados que o domínio produz após uma
// autenticação bem-sucedida. Diferente de um DTO (que é formatado para
// a API), este tipo pertence ao domínio e contém a entidade Usuario
// completa. O endpoint converte este resultado em um DTO de resposta,
// selecionando apenas os campos que devem ser expostos na API.
// ============================================================================

namespace Agendamentos.Entities.Models;

/// <summary>
/// Resultado de uma operação de login ou refresh token bem-sucedida.
///
/// Contém o par de tokens (Access + Refresh) e os dados do usuário.
/// O endpoint da API converte este resultado em um DTO de resposta,
/// mapeando o Usuario para apenas os campos públicos (sem SenhaHash).
/// </summary>
/// <param name="AccessToken">JWT para autenticação (vida curta).</param>
/// <param name="RefreshToken">Token para renovação (vida longa).</param>
/// <param name="ExpiraEm">Data/hora de expiração do Refresh Token.</param>
/// <param name="Usuario">Entidade completa do usuário autenticado.</param>
public record ResultadoLogin(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiraEm,
    Usuario Usuario
);
```

### 2.3 Common (Paginação)

Crie a pasta `Agendamentos.Entities/Common/`.

**Arquivo: `Agendamentos.Entities/Common/ListaPaginada.cs`**

```csharp
// ============================================================================
// Arquivo: Common/ListaPaginada.cs
// Descrição: Tipo genérico para resultados paginados com cursor (keyset pagination).
//
// CONCEITO IMPORTANTE (Paginação por Cursor vs Offset):
// Existem duas estratégias principais de paginação:
//
// 1. OFFSET (Skip/Take): Pula N registros e retorna os próximos M.
//    - Problema: conforme o offset cresce, a query fica mais lenta
//    - Problema: inserções/remoções entre páginas causam itens duplicados ou pulados
//
// 2. CURSOR (Keyset): Usa o ID do último item como referência para a próxima página.
//    - WHERE Id > @cursor ORDER BY Id ASC LIMIT @limite
//    - Performance constante independente da "página" (usa índice do banco)
//    - Estável: inserções/remoções não afetam a navegação
//    - Ideal para rolagem infinita (infinite scroll)
//
// O campo ProximoCursor contém o ID do último item retornado.
// O frontend envia esse valor na próxima requisição para obter a página seguinte.
// Quando TemMais é false, não há mais dados para carregar.
// ============================================================================

namespace Agendamentos.Entities.Common;

/// <summary>
/// Resultado paginado genérico usando cursor (keyset pagination).
/// Usado por todos os casos de uso que retornam listas paginadas.
/// </summary>
/// <typeparam name="T">Tipo da entidade paginada.</typeparam>
public class ListaPaginada<T>
{
    /// <summary>Itens da página atual.</summary>
    public required List<T> Itens { get; init; }

    /// <summary>
    /// Cursor para a próxima página (ID do último item).
    /// Null quando não há mais páginas.
    /// </summary>
    public long? ProximoCursor { get; init; }

    /// <summary>Indica se existem mais itens além desta página.</summary>
    public bool TemMais { get; init; }

    /// <summary>
    /// Cria uma ListaPaginada a partir de uma lista que contém limite+1 itens.
    /// O item extra é usado apenas para detectar se há mais páginas —
    /// ele NÃO é incluído no resultado.
    ///
    /// CONCEITO IMPORTANTE (Técnica do limite+1):
    /// Em vez de fazer COUNT(*) (que é caro), buscamos um item a mais do que
    /// o necessário. Se vieram limite+1 itens, sabemos que há mais. Se vieram
    /// menos, é a última página. Essa técnica é usada por GitHub, Twitter, etc.
    /// </summary>
    /// <param name="itensComExtra">Lista com até limite+1 itens.</param>
    /// <param name="limite">Quantidade de itens por página (sem o extra).</param>
    /// <param name="obterCursor">Função que extrai o ID (cursor) de um item.</param>
    public static ListaPaginada<T> Criar(
        List<T> itensComExtra,
        int limite,
        Func<T, long> obterCursor)
    {
        var temMais = itensComExtra.Count > limite;
        var itens = temMais
            ? itensComExtra.Take(limite).ToList()
            : itensComExtra;

        return new ListaPaginada<T>
        {
            Itens = itens,
            ProximoCursor = temMais && itens.Count > 0
                ? obterCursor(itens[^1])
                : null,
            TemMais = temMais
        };
    }
}
```

### ✅ COMMIT 2 — Entidades do domínio

- Se precisar do meu código para este commit, basta executar no repositório deste curso:

```bash
git checkout 9d68b4d
```

E para voltar para a última versão, basta executar:

```bash
git checkout main
```
