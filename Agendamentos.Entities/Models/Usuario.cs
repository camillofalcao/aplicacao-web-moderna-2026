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
