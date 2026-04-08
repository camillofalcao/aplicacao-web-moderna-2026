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
