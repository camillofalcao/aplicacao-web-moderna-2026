// ============================================================================
// Arquivo: UseCases/Prestadores/CriarPrestadorValidator.cs
// Descrição: Validador FluentValidation para a entidade Prestador (criação).
//
// CONCEITO IMPORTANTE (FluentValidation com Entidades):
// Este validador valida diretamente a entidade Prestador. O endpoint cria
// a entidade a partir dos dados da requisição, injeta o IValidator<Prestador>
// e chama ValidateAsync antes de chamar o caso de uso.
//
// Se a validação falhar, o caso de uso NÃO é executado e os erros
// de validação são retornados imediatamente ao cliente.
// ============================================================================

using Agendamentos.Entities.Models;
using FluentValidation;

namespace Agendamentos.Entities.UseCases.Prestadores;

/// <summary>
/// Validador para a entidade <see cref="Prestador"/>.
///
/// Valida os campos de entrada antes de chamar o caso de uso de criação.
/// As regras verificam formato e limites, NÃO regras de negócio.
/// </summary>
public class CriarPrestadorValidator : AbstractValidator<Prestador>
{
    public CriarPrestadorValidator()
    {
        // IDs no banco de dados são sempre positivos.
        RuleFor(x => x.UsuarioId)
            .GreaterThan(0)
            .WithMessage("O ID do usuário deve ser maior que zero.");

        // Especialidade obrigatória com mínimo de 3 caracteres.
        RuleFor(x => x.Especialidade)
            .NotEmpty()
            .WithMessage("A especialidade é obrigatória.")
            .MinimumLength(3)
            .WithMessage("A especialidade deve ter no mínimo 3 caracteres.");

        // Descrição obrigatória com mínimo de 10 caracteres.
        RuleFor(x => x.Descricao)
            .NotEmpty()
            .WithMessage("A descrição é obrigatória.")
            .MinimumLength(10)
            .WithMessage("A descrição deve ter no mínimo 10 caracteres.");

        // Valor da consulta deve ser positivo.
        RuleFor(x => x.ValorConsulta)
            .GreaterThan(0)
            .WithMessage("O valor da consulta deve ser maior que zero.");

        // Duração entre 15 e 480 minutos (8 horas).
        RuleFor(x => x.DuracaoAtendimentoMinutos)
            .InclusiveBetween(15, 480)
            .WithMessage("A duração do atendimento deve ser entre 15 e 480 minutos.");
    }
}
