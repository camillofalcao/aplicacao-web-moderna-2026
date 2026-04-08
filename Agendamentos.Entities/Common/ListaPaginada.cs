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
