// ============================================================================
// Arquivo: Models/Prestador.cs
// Descrição: Entidade que representa um prestador de serviços.
//            Contém informações profissionais e está vinculado a um Usuário.
//            Possui horários disponíveis e recebe agendamentos de clientes.
// ============================================================================

namespace Agendamentos.Entities.Models;

/// <summary>
/// Entidade de domínio que representa um prestador de serviços.
///
/// Um prestador é sempre vinculado a um Usuário com Role = Prestador.
/// O administrador cria o prestador, que recebe uma agenda própria
/// onde define seus horários disponíveis para atendimento.
///
/// Os dados do prestador são exibidos na página inicial em formato de cards,
/// e essa listagem é cacheada com Redis para melhor performance.
/// </summary>
public class Prestador
{
    /// <summary>
    /// Identificador único do prestador.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Chave estrangeira para o Usuário associado a este prestador.
    /// Cada prestador está vinculado a exatamente um usuário.
    /// </summary>
    public long UsuarioId { get; set; }

    /// <summary>
    /// Especialidade ou área de atuação do prestador.
    /// Exemplos: "Cabeleireiro", "Dentista", "Personal Trainer", "Advogado".
    /// </summary>
    public string Especialidade { get; set; } = string.Empty;

    /// <summary>
    /// Descrição detalhada dos serviços oferecidos pelo prestador.
    /// Exibida no card do prestador na página inicial.
    /// </summary>
    public string Descricao { get; set; } = string.Empty;

    /// <summary>
    /// URL da foto de perfil do prestador.
    /// Pode ser uma URL externa ou um caminho relativo para imagem local.
    /// </summary>
    public string? FotoUrl { get; set; }

    /// <summary>
    /// Valor cobrado por sessão/consulta em reais (R$).
    /// </summary>
    public decimal ValorConsulta { get; set; }

    /// <summary>
    /// Duração padrão de cada atendimento em minutos.
    /// Usado para calcular os slots disponíveis na agenda.
    /// </summary>
    public int DuracaoAtendimentoMinutos { get; set; } = 60;

    /// <summary>
    /// Indica se o prestador está ativo e aceitando novos agendamentos.
    /// </summary>
    public bool Ativo { get; set; } = true;

    // ---- Propriedades de Navegação ----

    /// <summary>
    /// Usuário vinculado a este prestador.
    /// Através dele acessamos Nome e Email do prestador.
    /// </summary>
    public Usuario Usuario { get; set; } = null!;

    /// <summary>
    /// Horários disponíveis para atendimento definidos pelo prestador.
    /// Cada registro define um dia da semana com hora de início e fim.
    /// </summary>
    public List<HorarioDisponivel> HorariosDisponiveis { get; set; } = [];

    /// <summary>
    /// Agendamentos marcados com este prestador.
    /// </summary>
    public List<Agendamento> Agendamentos { get; set; } = [];
}
