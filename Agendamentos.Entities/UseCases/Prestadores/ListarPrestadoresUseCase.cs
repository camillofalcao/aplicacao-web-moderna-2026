// ============================================================================
// Arquivo: UseCases/Prestadores/ListarPrestadoresUseCase.cs
// Descrição: Caso de uso para listagem de prestadores ativos COM CACHE.
//
// CONCEITO IMPORTANTE (Cache com Redis via ICachePrestadores):
// Cache é uma técnica fundamental para melhorar a performance de APIs.
// A ideia é armazenar dados frequentemente acessados em memória (RAM),
// evitando consultas repetidas ao banco de dados.
//
// ESTRATÉGIA DE CACHE UTILIZADA (Cache-Aside / Lazy Loading):
// 1. O caso de uso PRIMEIRO verifica se os dados estão no cache (cache hit)
// 2. Se SIM → retorna os dados do cache (muito rápido, sem ir ao banco)
// 3. Se NÃO (cache miss) → busca no banco, armazena no cache e retorna
//
// CONCEITO IMPORTANTE (ICachePrestadores — Inversão de Dependência):
// Em vez de depender diretamente de IDistributedCache e fazer serialização
// JSON manualmente, o caso de uso usa ICachePrestadores (interface do domínio).
// A implementação concreta lida com Redis, serialização e TTL internamente.
// ============================================================================

using Agendamentos.Entities.Common;
using Agendamentos.Entities.Interfaces;
using Agendamentos.Entities.Models;
using ErrorOr;

namespace Agendamentos.Entities.UseCases.Prestadores;

/// <summary>
/// Caso de uso responsável por listar prestadores ativos com cache e paginação.
///
/// Implementa Cache-Aside + paginação em memória:
/// 1. Busca a lista completa do cache (ou banco se cache miss)
/// 2. Pagina o resultado em memória usando cursor (keyset)
///
/// CONCEITO IMPORTANTE (Paginação em memória vs no banco):
/// Para prestadores, a paginação é feita EM MEMÓRIA após obter a lista
/// completa do cache. Isso é eficiente porque:
/// - A lista de prestadores é tipicamente pequena (dezenas, não milhares)
/// - O cache Redis já armazena a lista completa (TTL de 1 minuto)
/// - Paginar no banco exigiria cache por página (mais complexo)
///
/// Para entidades com volume maior (Agendamentos, Usuários), a paginação
/// é feita diretamente no banco de dados via keyset pagination.
/// </summary>
public class ListarPrestadoresUseCase
{
    private readonly IPrestadorRepository _prestadorRepository;
    private readonly ICachePrestadores _cache;

    public ListarPrestadoresUseCase(
        IPrestadorRepository prestadorRepository,
        ICachePrestadores cache)
    {
        _prestadorRepository = prestadorRepository;
        _cache = cache;
    }

    /// <summary>
    /// Executa a listagem paginada de prestadores ativos com cache.
    ///
    /// FLUXO COMPLETO:
    /// 1. Tenta buscar a lista completa do cache (rápido, em memória)
    /// 2. Se cache miss → busca no banco e armazena no cache
    /// 3. Aplica paginação por cursor na lista em memória
    /// 4. Retorna o resultado paginado
    /// </summary>
    /// <param name="cursor">ID do último item da página anterior (null para primeira página).</param>
    /// <param name="limite">Quantidade de itens por página.</param>
    /// <param name="ct">Token de cancelamento para operações assíncronas.</param>
    public async Task<ErrorOr<ListaPaginada<Prestador>>> ExecutarAsync(
        int? cursor, int limite, CancellationToken ct)
    {
        // =============================================================
        // PASSO 1: Obter lista completa (cache ou banco)
        // =============================================================
        List<Prestador>? prestadores = null;
        try
        {
            prestadores = await _cache.ObterListaAsync(ct);
        }
        catch
        {
            // Cache indisponível — segue para o banco de dados.
        }

        if (prestadores is null)
        {
            prestadores = await _prestadorRepository.ListarAtivosAsync();

            try
            {
                await _cache.ArmazenarListaAsync(prestadores, ct);
            }
            catch
            {
                // Cache indisponível — retorna dados do banco normalmente.
            }
        }

        // =============================================================
        // PASSO 2: Paginar em memória usando cursor
        // =============================================================
        IEnumerable<Prestador> query = prestadores.OrderBy(p => p.Id);

        if (cursor.HasValue)
            query = query.Where(p => p.Id > cursor.Value);

        var pagina = query.Take(limite + 1).ToList();

        return ListaPaginada<Prestador>.Criar(pagina, limite, p => p.Id);
    }
}
