// ============================================================================
// Arquivo: UseCases/Common/Erros.cs
// Descrição: Classe estática que centraliza todos os erros do sistema.
//
// CONCEITO IMPORTANTE (ErrorOr):
// A biblioteca ErrorOr permite retornar erros SEM lançar exceções.
// Em vez de "throw new Exception", retornamos um Error tipado.
// Isso torna o código mais previsível e performático, pois exceções
// são caras em termos de performance e devem ser reservadas para
// situações verdadeiramente excepcionais.
//
// Cada Error tem:
// - Code: código único para identificação programática
// - Description: mensagem legível para o desenvolvedor/usuário
// - Type: tipo do erro (Validation, NotFound, Conflict, Unauthorized, etc.)
// ============================================================================

using ErrorOr;

namespace Agendamentos.Entities.UseCases.Common;

/// <summary>
/// Centraliza todos os erros de domínio do sistema.
/// Organizado por entidade para fácil localização.
///
/// Usar uma classe estática para erros garante:
/// 1. Consistência nas mensagens de erro
/// 2. Reuso em diferentes casos de uso
/// 3. Fácil manutenção e internacionalização futura
/// </summary>
public static class Erros
{
    /// <summary>
    /// Erros relacionados à entidade Usuário.
    /// </summary>
    public static class Usuario
    {
        public static Error EmailJaExiste => Error.Conflict(
            code: "Usuario.EmailJaExiste",
            description: "Já existe um usuário cadastrado com este e-mail.");

        public static Error NaoEncontrado => Error.NotFound(
            code: "Usuario.NaoEncontrado",
            description: "Usuário não encontrado.");

        public static Error SenhaInvalida => Error.Validation(
            code: "Usuario.SenhaInvalida",
            description: "E-mail ou senha inválidos.");

        public static Error Inativo => Error.Forbidden(
            code: "Usuario.Inativo",
            description: "Este usuário está desativado.");
    }

    /// <summary>
    /// Erros relacionados à entidade Prestador.
    /// </summary>
    public static class Prestador
    {
        public static Error NaoEncontrado => Error.NotFound(
            code: "Prestador.NaoEncontrado",
            description: "Prestador não encontrado.");

        public static Error UsuarioJaEhPrestador => Error.Conflict(
            code: "Prestador.UsuarioJaEhPrestador",
            description: "Este usuário já está cadastrado como prestador.");
    }

    /// <summary>
    /// Erros relacionados à entidade Agendamento.
    /// </summary>
    public static class Agendamento
    {
        public static Error NaoEncontrado => Error.NotFound(
            code: "Agendamento.NaoEncontrado",
            description: "Agendamento não encontrado.");

        public static Error ConflitoHorario => Error.Conflict(
            code: "Agendamento.ConflitoHorario",
            description: "Já existe um agendamento neste horário.");

        public static Error HorarioIndisponivel => Error.Validation(
            code: "Agendamento.HorarioIndisponivel",
            description: "O prestador não atende neste dia/horário.");

        public static Error NaoPodeCancelar => Error.Validation(
            code: "Agendamento.NaoPodeCancelar",
            description: "Este agendamento não pode ser cancelado.");

        public static Error AcessoNegado => Error.Forbidden(
            code: "Agendamento.AcessoNegado",
            description: "Você não tem permissão para acessar este agendamento.");
    }

    /// <summary>
    /// Erros relacionados à autenticação e tokens.
    /// </summary>
    public static class Auth
    {
        public static Error RefreshTokenInvalido => Error.Validation(
            code: "Auth.RefreshTokenInvalido",
            description: "Refresh token inválido ou expirado.");

        public static Error NaoAutorizado => Error.Unauthorized(
            code: "Auth.NaoAutorizado",
            description: "Você não está autenticado.");
    }
}
