// ============================================================================
// Arquivo: UseCases/Horarios/ListarHorariosUseCase.cs
// Descrição: Caso de uso para listagem de horários disponíveis de um prestador.
//
// CONCEITO IMPORTANTE (Caso de Uso Simples):
// Nem todo caso de uso precisa de lógica complexa. Este apenas busca dados
// e os retorna. Em Clean Architecture, mesmo operações simples passam pela
// camada de casos de uso para manter a consistência do fluxo e garantir
// que toda lógica de negócio esteja centralizada.
// ============================================================================

using Agendamentos.Entities.Interfaces;
using Agendamentos.Entities.Models;
using ErrorOr;

namespace Agendamentos.Entities.UseCases.Horarios;

/// <summary>
/// Caso de uso responsável por listar os horários disponíveis de um prestador.
/// Retorna as entidades HorarioDisponivel — o endpoint mapeia para DTO.
/// </summary>
public class ListarHorariosUseCase
{
    private readonly IHorarioDisponivelRepository _horarioRepo;

    public ListarHorariosUseCase(IHorarioDisponivelRepository horarioRepo)
    {
        _horarioRepo = horarioRepo;
    }

    /// <summary>
    /// Executa a listagem de horários disponíveis de um prestador.
    /// </summary>
    /// <param name="prestadorId">ID do prestador cujos horários serão listados.</param>
    /// <param name="ct">Token de cancelamento.</param>
    public async Task<ErrorOr<List<HorarioDisponivel>>> ExecutarAsync(
        int prestadorId,
        CancellationToken ct)
    {
        var horarios = await _horarioRepo.ListarPorPrestadorIdAsync(prestadorId);
        return horarios;
    }
}
