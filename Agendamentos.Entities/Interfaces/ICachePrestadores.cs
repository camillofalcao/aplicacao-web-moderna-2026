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
