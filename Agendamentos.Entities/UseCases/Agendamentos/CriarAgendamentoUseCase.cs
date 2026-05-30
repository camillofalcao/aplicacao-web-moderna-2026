// ============================================================================
// Arquivo: UseCases/Agendamentos/CriarAgendamentoUseCase.cs
// Descrição: Caso de uso para criação de um novo agendamento.
//
// CONCEITO IMPORTANTE (Use Case com Validações de Negócio):
// Este caso de uso demonstra múltiplas validações de negócio antes
// de permitir a criação de um agendamento:
// 1. O prestador deve existir e estar ativo
// 2. O dia da semana e horário devem estar nos horários disponíveis
// 3. Não pode haver conflito com outro agendamento existente
//
// CONCEITO IMPORTANTE (Rich Domain Model):
// A verificação de horário usa o método Contem() da entidade
// HorarioDisponivel, encapsulando a lógica de "o horário do agendamento
// está dentro do expediente do prestador" na própria entidade.
//
// FLUXO DE VALIDAÇÃO (3 etapas):
// 1. Validator (FluentValidation) → valida formato dos dados (roda antes)
// 2. Caso de uso → valida regras de negócio (prestador ativo? horário ok?)
// 3. Banco de dados → constraints e foreign keys (última linha de defesa)
// ============================================================================

using Agendamentos.Entities.Enums;
using Agendamentos.Entities.Interfaces;
using Agendamentos.Entities.Models;
using Agendamentos.Entities.UseCases.Common;
using ErrorOr;

namespace Agendamentos.Entities.UseCases.Agendamentos;

/// <summary>
/// Caso de uso responsável por criar um novo agendamento.
///
/// Recebe a entidade Agendamento com PrestadorId, ClienteId, DataHora
/// e Observacoes preenchidos pelo endpoint. O caso de uso configura
/// o Status inicial e CriadoEm, além de executar todas as validações.
/// </summary>
public class CriarAgendamentoUseCase
{
    private readonly IAgendamentoRepository _agendamentoRepo;
    private readonly IPrestadorRepository _prestadorRepo;
    private readonly IHorarioDisponivelRepository _horarioRepo;
    private readonly IUnitOfWork _unitOfWork;

    public CriarAgendamentoUseCase(
        IAgendamentoRepository agendamentoRepo,
        IPrestadorRepository prestadorRepo,
        IHorarioDisponivelRepository horarioRepo,
        IUnitOfWork unitOfWork)
    {
        _agendamentoRepo = agendamentoRepo;
        _prestadorRepo = prestadorRepo;
        _horarioRepo = horarioRepo;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Executa a criação de um novo agendamento.
    /// </summary>
    /// <param name="agendamento">
    ///     Entidade Agendamento com PrestadorId, ClienteId, DataHora
    ///     e Observacoes preenchidos. Status e CriadoEm são configurados
    ///     por este caso de uso.
    /// </param>
    /// <param name="ct">Token de cancelamento.</param>
    /// <returns>
    ///     ErrorOr contendo a entidade Agendamento em caso de sucesso,
    ///     ou erros em caso de falha na validação de negócio.
    /// </returns>
    public async Task<ErrorOr<Agendamento>> ExecutarAsync(
        Agendamento agendamento,
        CancellationToken ct)
    {
        // ---------------------------------------------------------------
        // ETAPA 1: Verificar se o prestador existe e está ativo
        // ---------------------------------------------------------------
        var prestador = await _prestadorRepo.ObterPorIdAsync(agendamento.PrestadorId);

        if (prestador is null)
            return Erros.Prestador.NaoEncontrado;

        if (!prestador.Ativo)
            return Erros.Prestador.NaoEncontrado;

        // ---------------------------------------------------------------
        // ETAPA 2: Verificar se o prestador atende neste dia/horário
        // ---------------------------------------------------------------
        // Convertemos a DataHora do agendamento para DayOfWeek + TimeOnly
        // e usamos o método Contem() da entidade HorarioDisponivel
        // para verificar se o horário está dentro do expediente.
        var diaSemana = agendamento.DataHora.DayOfWeek;
        var horaAgendamento = TimeOnly.FromDateTime(agendamento.DataHora);

        var horariosDisponiveis = await _horarioRepo.ListarPorPrestadorIdAsync(agendamento.PrestadorId);

        // Rich Domain Model: usa o método Contem() da entidade
        var horarioValido = horariosDisponiveis.Any(h =>
            h.Contem(diaSemana, horaAgendamento));

        if (!horarioValido)
            return Erros.Agendamento.HorarioIndisponivel;

        // ---------------------------------------------------------------
        // ETAPA 3: Verificar se não há conflito de horário
        // ---------------------------------------------------------------
        // Dois clientes não podem agendar no mesmo horário com o mesmo prestador.
        var existeConflito = await _agendamentoRepo.ExisteConflito(
            agendamento.PrestadorId, agendamento.DataHora);

        if (existeConflito)
            return Erros.Agendamento.ConflitoHorario;

        // ---------------------------------------------------------------
        // ETAPA 4: Configurar valores padrão e criar o agendamento
        // ---------------------------------------------------------------
        agendamento.Status = EStatusAgendamento.Pendente;
        agendamento.CriadoEm = DateTime.UtcNow;

        await _agendamentoRepo.AdicionarAsync(agendamento);
        await _unitOfWork.SaveChangesAsync(ct);

        // ---------------------------------------------------------------
        // ETAPA 5: Buscar o agendamento completo para a resposta
        // ---------------------------------------------------------------
        // Após salvar, buscamos novamente com os dados de navegação
        // (Prestador e Cliente) para que o endpoint possa montar o DTO.
        var agendamentoCriado = await _agendamentoRepo.ObterPorIdAsync(agendamento.Id);

        return agendamentoCriado!;
    }
}
