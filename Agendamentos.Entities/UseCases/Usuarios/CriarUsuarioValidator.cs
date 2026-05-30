// ============================================================================
// Arquivo: UseCases/Usuarios/CriarUsuarioValidator.cs
// Descrição: Validador FluentValidation para a entidade Usuario (criação).
//
// CONCEITO IMPORTANTE (FluentValidation):
// FluentValidation é uma biblioteca que permite definir regras de validação
// usando uma API fluente (encadeamento de métodos). Vantagens:
// 1. Separação de responsabilidades: a validação fica fora do caso de uso
// 2. Regras expressivas e legíveis ("RuleFor(x => x.Nome).NotEmpty()")
// 3. Mensagens de erro customizáveis
// 4. Fácil de testar isoladamente
//
// CONCEITO IMPORTANTE (Validação vs. Regra de Negócio):
// - Validação (feita aqui): formato dos dados (campo obrigatório, tamanho)
// - Regra de negócio (feita no caso de uso): lógica do domínio (e-mail já existe?)
// A validação é "barata" e acontece antes de acessar o banco de dados.
//
// NOTA: A senha é validada no endpoint porque não faz parte da entidade
// Usuario (a entidade armazena SenhaHash, não a senha em texto plano).
// ============================================================================

using Agendamentos.Entities.Models;
using FluentValidation;

namespace Agendamentos.Entities.UseCases.Usuarios;

/// <summary>
/// Validador que define as regras de validação para a entidade <see cref="Usuario"/>.
///
/// Herda de AbstractValidator&lt;Usuario&gt; que é a classe base do FluentValidation.
/// As regras são definidas no construtor usando a API fluente.
///
/// O endpoint injeta IValidator&lt;Usuario&gt; e chama ValidateAsync antes
/// de chamar o caso de uso. Se a validação falhar, o caso de uso NÃO é executado.
/// </summary>
public class CriarUsuarioValidator : AbstractValidator<Usuario>
{
    public CriarUsuarioValidator()
    {
        // ---------------------------------------------------------------
        // Regras para o campo Nome
        // ---------------------------------------------------------------
        // NotEmpty(): verifica se NÃO é null, vazio ("") ou whitespace ("   ")
        // MinimumLength(3): verifica se tem pelo menos 3 caracteres
        RuleFor(u => u.Nome)
            .NotEmpty()
                .WithMessage("O nome é obrigatório.")
            .MinimumLength(3)
                .WithMessage("O nome deve ter pelo menos 3 caracteres.");

        // ---------------------------------------------------------------
        // Regras para o campo Email
        // ---------------------------------------------------------------
        // NotEmpty(): campo obrigatório
        // EmailAddress(): valida o formato de e-mail (contém @, domínio, etc.)
        RuleFor(u => u.Email)
            .NotEmpty()
                .WithMessage("O e-mail é obrigatório.")
            .EmailAddress()
                .WithMessage("O e-mail informado não é válido.");
    }
}
