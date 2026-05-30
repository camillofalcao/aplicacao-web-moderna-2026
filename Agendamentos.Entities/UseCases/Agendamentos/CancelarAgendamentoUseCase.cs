// ============================================================================
// Arquivo: UseCases/Agendamentos/CancelarAgendamentoUseCase.cs
// Descrição: Caso de uso para cancelamento de um agendamento existente.
//
// CONCEITO IMPORTANTE (Rich Domain Model — Transição de Estado):
// Este caso de uso demonstra o uso de entidades enriquecidas:
// - PodeCancelar: propriedade que verifica se o status permite cancelamento
// - PertenceAoUsuario(): método que verifica autorização a nível de recurso
// - Cancelar(): método que executa a transição de estado
//
// As regras de negócio estão encapsuladas na entidade, e o caso de uso
// as orquestra na ordem correta.
//
// CONCEITO IMPORTANTE (Autorização no Caso de Uso):
// A autorização é feita no caso de uso (e não apenas no endpoint) porque
// é uma regra de NEGÓCIO: "somente as partes envolvidas podem cancelar".
// O endpoint verifica se o usuário está autenticado (AuthN),
// mas quem verifica se ele TEM PERMISSÃO para esta ação específica
// é o caso de uso (AuthZ a nível de recurso).
// ============================================================================

using Agendamentos.Entities.Interfaces;
using Agendamentos.Entities.Models;
using Agendamentos.Entities.UseCases.Common;
using ErrorOr;

namespace Agendamentos.Entities.UseCases.Agendamentos;

/// <summary>
/// Caso de uso responsável por cancelar um agendamento existente.
///
/// Usa métodos da entidade Agendamento (Rich Domain Model):
/// - PodeCancelar: verifica se o status permite cancelamento
/// - PertenceAoUsuario(): verifica autorização a nível de recurso
/// - Cancelar(): executa a transição de estado para Cancelado
/// </summary>
public class CancelarAgendamentoUseCase
{
    private readonly IAgendamentoRepository _agendamentoRepo;
    private readonly IUnitOfWork _unitOfWork;

    public CancelarAgendamentoUseCase(
        IAgendamentoRepository agendamentoRepo,
        IUnitOfWork unitOfWork)
    {
        _agendamentoRepo = agendamentoRepo;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Executa o cancelamento de um agendamento.
    /// </summary>
    /// <param name="agendamentoId">ID do agendamento a cancelar.</param>
    /// <param name="usuarioId">ID do usuário que está cancelando (do JWT).</param>
    /// <param name="ct">Token de cancelamento.</param>
    public async Task<ErrorOr<Agendamento>> ExecutarAsync(
        int agendamentoId,
        int usuarioId,
        CancellationToken ct)
    {
        // ---------------------------------------------------------------
        // ETAPA 1: Verificar se o agendamento existe
        // ---------------------------------------------------------------
        var agendamento = await _agendamentoRepo.ObterPorIdAsync(agendamentoId);

        if (agendamento is null)
            return Erros.Agendamento.NaoEncontrado;

        // ---------------------------------------------------------------
        // ETAPA 2: Verificar permissão do usuário (Rich Domain Model)
        // ---------------------------------------------------------------
        // O método PertenceAoUsuario() da entidade verifica se o usuário
        // é o cliente ou o prestador do agendamento.
        if (!agendamento.PertenceAoUsuario(usuarioId, agendamento.Prestador.UsuarioId))
            return Erros.Agendamento.AcessoNegado;

        // ---------------------------------------------------------------
        // ETAPA 3: Verificar se o status permite cancelamento (Rich Domain Model)
        // ---------------------------------------------------------------
        // A propriedade PodeCancelar da entidade encapsula a regra:
        // apenas agendamentos Pendente ou Confirmado podem ser cancelados.
        if (!agendamento.PodeCancelar)
            return Erros.Agendamento.NaoPodeCancelar;

        // ---------------------------------------------------------------
        // ETAPA 4: Cancelar o agendamento (Rich Domain Model)
        // ---------------------------------------------------------------
        // O método Cancelar() da entidade altera o Status para Cancelado.
        agendamento.Cancelar();
        await _agendamentoRepo.AtualizarAsync(agendamento);
        await _unitOfWork.SaveChangesAsync(ct);

        // ---------------------------------------------------------------
        // ETAPA 5: Retornar a entidade atualizada
        // ---------------------------------------------------------------
        return agendamento;
    }
}
