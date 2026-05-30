// ============================================================================
// Arquivo: UseCases/Horarios/DefinirHorarioUseCase.cs
// Descrição: Caso de uso para definição de horário disponível de um prestador.
//
// CONCEITO IMPORTANTE (Validação no Caso de Uso vs Endpoint):
// O endpoint é responsável por converter os dados de entrada (ex: strings
// de horário "08:00" → TimeOnly) e criar a entidade HorarioDisponivel.
// O caso de uso é responsável por validar as regras de NEGÓCIO
// (ex: o prestador existe?).
//
// Essa separação garante que cada camada cuide das suas responsabilidades:
// - API: conversão de tipos, validação de formato
// - Use Case: regras de negócio
// ============================================================================

using Agendamentos.Entities.Interfaces;
using Agendamentos.Entities.Models;
using Agendamentos.Entities.UseCases.Common;
using ErrorOr;

namespace Agendamentos.Entities.UseCases.Horarios;

/// <summary>
/// Caso de uso responsável por definir um horário disponível para um prestador.
///
/// Recebe a entidade HorarioDisponivel já com os campos TimeOnly preenchidos
/// (a conversão de string → TimeOnly é feita pelo endpoint).
/// </summary>
public class DefinirHorarioUseCase
{
    private readonly IHorarioDisponivelRepository _horarioRepo;
    private readonly IPrestadorRepository _prestadorRepo;
    private readonly IUnitOfWork _unitOfWork;

    public DefinirHorarioUseCase(
        IHorarioDisponivelRepository horarioRepo,
        IPrestadorRepository prestadorRepo,
        IUnitOfWork unitOfWork)
    {
        _horarioRepo = horarioRepo;
        _prestadorRepo = prestadorRepo;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Executa a definição de um horário disponível.
    /// </summary>
    /// <param name="horario">
    ///     Entidade HorarioDisponivel com PrestadorId, DiaSemana,
    ///     HoraInicio e HoraFim já convertidos para TimeOnly.
    /// </param>
    /// <param name="ct">Token de cancelamento.</param>
    public async Task<ErrorOr<HorarioDisponivel>> ExecutarAsync(
        HorarioDisponivel horario,
        CancellationToken ct)
    {
        // ---------------------------------------------------------------
        // ETAPA 1: Verificar se o prestador existe
        // ---------------------------------------------------------------
        var prestador = await _prestadorRepo.ObterPorIdAsync(horario.PrestadorId);

        if (prestador is null)
            return Erros.Prestador.NaoEncontrado;

        // ---------------------------------------------------------------
        // ETAPA 2: Salvar o horário disponível
        // ---------------------------------------------------------------
        await _horarioRepo.AdicionarAsync(horario);
        await _unitOfWork.SaveChangesAsync(ct);

        // ---------------------------------------------------------------
        // ETAPA 3: Retornar a entidade criada
        // ---------------------------------------------------------------
        return horario;
    }
}
