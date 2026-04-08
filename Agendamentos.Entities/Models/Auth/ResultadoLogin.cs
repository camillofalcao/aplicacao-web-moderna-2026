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