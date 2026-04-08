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
