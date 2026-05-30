// ============================================================================
// Arquivo: UseCases/Prestadores/ObterPrestadorUseCase.cs
// Descrição: Caso de uso para obtenção de um prestador específico pelo ID.
//
// CONCEITO IMPORTANTE (Caso de Uso sem Cache):
// Para consultas por ID (que retornam um único registro), o cache
// geralmente não é necessário porque:
// 1. A consulta por chave primária já é muito rápida no banco (índice)
// 2. O volume de requisições por ID específico tende a ser menor
// 3. Gerenciar cache por ID individual aumenta a complexidade
// ============================================================================

using Agendamentos.Entities.Interfaces;
using Agendamentos.Entities.Models;
using Agendamentos.Entities.UseCases.Common;
using ErrorOr;

namespace Agendamentos.Entities.UseCases.Prestadores;

/// <summary>
/// Caso de uso responsável por buscar os dados de um prestador pelo ID.
/// Diferente do ListarPrestadoresUseCase, este NÃO usa cache.
/// Retorna a entidade Prestador — o endpoint mapeia para DTO.
/// </summary>
public class ObterPrestadorUseCase
{
    private readonly IPrestadorRepository _prestadorRepository;

    public ObterPrestadorUseCase(IPrestadorRepository prestadorRepository)
    {
        _prestadorRepository = prestadorRepository;
    }

    /// <summary>
    /// Executa a busca de um prestador pelo ID.
    /// </summary>
    /// <param name="id">ID do prestador a ser consultado.</param>
    /// <param name="ct">Token de cancelamento.</param>
    /// <returns>
    ///     ErrorOr contendo a entidade Prestador em caso de sucesso,
    ///     ou Error.NotFound se o prestador não existir.
    /// </returns>
    public async Task<ErrorOr<Prestador>> ExecutarAsync(
        int id,
        CancellationToken ct)
    {
        var prestador = await _prestadorRepository.ObterPorIdAsync(id);

        if (prestador is null)
            return Erros.Prestador.NaoEncontrado;

        return prestador;
    }
}
