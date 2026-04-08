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
