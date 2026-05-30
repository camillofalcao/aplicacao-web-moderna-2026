// ============================================================================
// Arquivo: UseCases/Agendamentos/CriarAgendamentoValidator.cs
// Descrição: Validador FluentValidation para a entidade Agendamento (criação).
//
// CONCEITO IMPORTANTE (Validação em Camadas):
// Este validador realiza validações "superficiais" (formato, limites):
// - PrestadorId e ClienteId devem ser positivos
// - DataHora deve ser no futuro
//
// Validações de "negócio" (prestador existe? horário disponível? conflito?)
// são feitas no caso de uso, que tem acesso aos repositórios.
// ============================================================================

using Agendamentos.Entities.Models;
using FluentValidation;

namespace Agendamentos.Entities.UseCases.Agendamentos;

/// <summary>
/// Validador para a entidade <see cref="Agendamento"/>.
///
/// Valida os campos de entrada antes de chamar o caso de uso de criação.
/// </summary>
public class CriarAgendamentoValidator : AbstractValidator<Agendamento>
{
    public CriarAgendamentoValidator()
    {
        // O identificador do prestador deve ser positivo.
        RuleFor(a => a.PrestadorId)
            .GreaterThan(0)
            .WithMessage("O prestador deve ser informado.");

        // O identificador do cliente deve ser positivo.
        RuleFor(a => a.ClienteId)
            .GreaterThan(0)
            .WithMessage("O cliente deve ser informado.");

        // A data/hora do agendamento deve ser no futuro.
        RuleFor(a => a.DataHora)
            .GreaterThan(DateTime.UtcNow)
            .WithMessage("A data e hora do agendamento devem ser no futuro.");
    }
}
