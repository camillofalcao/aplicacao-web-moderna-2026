// ============================================================================
// Arquivo: UseCases/Horarios/DefinirHorarioValidator.cs
// Descrição: Validador FluentValidation para a entidade HorarioDisponivel.
//
// CONCEITO IMPORTANTE (Validação de Entidade com TimeOnly):
// Como a entidade HorarioDisponivel usa TimeOnly para HoraInicio e HoraFim,
// a validação de formato de string ("HH:mm") não é mais necessária aqui —
// a conversão de string → TimeOnly é responsabilidade do endpoint (API).
//
// Este validador foca nas regras que podem ser verificadas na entidade:
// - PrestadorId é positivo?
// - A hora de fim é posterior à hora de início?
// ============================================================================

using Agendamentos.Entities.Models;
using FluentValidation;

namespace Agendamentos.Entities.UseCases.Horarios;

/// <summary>
/// Validador para a entidade <see cref="HorarioDisponivel"/>.
///
/// Valida os campos da entidade antes de chamar o caso de uso.
/// A validação de formato de horário (string → TimeOnly) é feita
/// no endpoint, pois a entidade já usa TimeOnly.
/// </summary>
public class DefinirHorarioValidator : AbstractValidator<HorarioDisponivel>
{
    public DefinirHorarioValidator()
    {
        // O identificador do prestador deve ser positivo.
        RuleFor(h => h.PrestadorId)
            .GreaterThan(0)
            .WithMessage("O prestador deve ser informado.");

        // A hora de fim deve ser posterior à hora de início.
        // Como ambos são TimeOnly, a comparação é direta e segura.
        RuleFor(h => h.HoraFim)
            .GreaterThan(h => h.HoraInicio)
            .WithMessage("A hora de fim deve ser posterior à hora de início.");
    }
}
