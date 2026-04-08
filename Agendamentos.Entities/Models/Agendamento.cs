// ============================================================================
// Arquivo: Models/Agendamento.cs
// Descrição: Entidade que representa um agendamento entre cliente e prestador.
//            Contém data/hora, status e observações sobre o serviço agendado.
// ============================================================================

using Agendamentos.Entities.Enums;

namespace Agendamentos.Entities.Models;

/// <summary>
/// Entidade de domínio que representa um agendamento de serviço.
///
/// Um agendamento vincula um Cliente a um Prestador em uma data/hora específica.
/// Possui um ciclo de vida representado pelo EStatusAgendamento:
///   Pendente → Confirmado → Concluido (fluxo normal)
///   Pendente/Confirmado → Cancelado (cancelamento)
/// </summary>
public class Agendamento
{
    /// <summary>
    /// Identificador único do agendamento.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Chave estrangeira para o Prestador de serviço.
    /// </summary>
    public long PrestadorId { get; set; }

    /// <summary>
    /// Chave estrangeira para o Cliente (Usuário) que agendou.
    /// </summary>
    public long ClienteId { get; set; }

    /// <summary>
    /// Data e hora marcada para o atendimento.
    /// Armazenada em UTC para evitar problemas com fuso horário.
    /// </summary>
    public DateTime DataHora { get; set; }

    /// <summary>
    /// Status atual do agendamento (Pendente, Confirmado, Cancelado, Concluido).
    /// </summary>
    public EStatusAgendamento Status { get; set; } = EStatusAgendamento.Pendente;

    /// <summary>
    /// Observações adicionais feitas pelo cliente ao agendar.
    /// Exemplo: "Preciso de corte e barba" ou "Consulta de retorno".
    /// </summary>
    public string? Observacoes { get; set; }

    /// <summary>
    /// Data e hora em que o agendamento foi criado.
    /// </summary>
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    // ---- Comportamento (Rich Domain Model) ----

    /// <summary>
    /// Indica se o agendamento pode ser cancelado.
    /// Apenas agendamentos nos status Pendente ou Confirmado podem ser cancelados.
    /// Cancelado e Concluído são estados finais e imutáveis.
    /// </summary>
    public bool PodeCancelar => Status is EStatusAgendamento.Pendente
                                or EStatusAgendamento.Confirmado;

    /// <summary>
    /// Verifica se o agendamento pertence ao usuário informado,
    /// seja como cliente (ClienteId) ou como prestador (UsuarioId do prestador).
    /// Usado para autorização a nível de recurso.
    /// </summary>
    /// <param name="usuarioId">ID do usuário logado.</param>
    /// <param name="prestadorUsuarioId">ID do usuário vinculado ao prestador.</param>
    public bool PertenceAoUsuario(long usuarioId, long prestadorUsuarioId)
        => ClienteId == usuarioId || prestadorUsuarioId == usuarioId;

    /// <summary>
    /// Cancela o agendamento, alterando seu status para Cancelado.
    /// Deve-se verificar PodeCancelar antes de chamar este método.
    /// </summary>
    public void Cancelar() => Status = EStatusAgendamento.Cancelado;

    // ---- Propriedades de Navegação ----

    /// <summary>
    /// Prestador de serviço associado a este agendamento.
    /// </summary>
    public Prestador Prestador { get; set; } = null!;

    /// <summary>
    /// Cliente (Usuário) que criou este agendamento.
    /// </summary>
    public Usuario Cliente { get; set; } = null!;
}
