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
    /// Identificador único do refresh token.
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