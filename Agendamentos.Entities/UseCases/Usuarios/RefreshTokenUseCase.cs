// ============================================================================
// Arquivo: UseCases/Usuarios/RefreshTokenUseCase.cs
// Descrição: Caso de uso para renovação de tokens via Refresh Token.
//
// CONCEITO IMPORTANTE (Fluxo de Renovação de Tokens):
// 1. O Access Token (JWT) do cliente expirou
// 2. O cliente envia o Refresh Token para este endpoint
// 3. O servidor busca o Refresh Token no banco de dados
// 4. Verifica se o token é válido (não expirado, não revogado)
// 5. Revoga o token antigo (marca como usado)
// 6. Gera um NOVO Access Token + NOVO Refresh Token
// 7. Retorna os novos tokens ao cliente
//
// CONCEITO IMPORTANTE (Rich Domain Model):
// A revogação do token é feita pelo método Revogar() da entidade
// RefreshToken, que encapsula a lógica de marcar RevogadoEm.
// A verificação de atividade usa a propriedade Ativo da entidade.
// ============================================================================

using Agendamentos.Entities.Interfaces;
using Agendamentos.Entities.Models;
using Agendamentos.Entities.Models.Auth;
using Agendamentos.Entities.UseCases.Common;
using ErrorOr;

namespace Agendamentos.Entities.UseCases.Usuarios;

/// <summary>
/// Caso de uso responsável por renovar tokens usando Refresh Token.
///
/// Fluxo de execução:
/// 1. Busca o usuário que possui o Refresh Token informado
/// 2. Localiza o token específico na lista de tokens do usuário
/// 3. Verifica se o token está ativo (não expirado e não revogado)
/// 4. Revoga o token antigo usando o método da entidade (Revogar)
/// 5. Gera novos tokens (Access Token + Refresh Token)
/// 6. Salva o novo Refresh Token no banco
/// 7. Retorna o ResultadoLogin com tokens atualizados
/// </summary>
public class RefreshTokenUseCase
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly ITokenService _tokenService;
    private readonly IUnitOfWork _unitOfWork;

    public RefreshTokenUseCase(
        IUsuarioRepository usuarioRepository,
        ITokenService tokenService,
        IUnitOfWork unitOfWork)
    {
        _usuarioRepository = usuarioRepository;
        _tokenService = tokenService;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Executa a renovação de tokens usando um Refresh Token válido.
    ///
    /// Este caso de uso recebe o token como string, pois o Refresh Token
    /// não é uma entidade de domínio — é um valor que referencia um registro
    /// no banco de dados.
    /// </summary>
    /// <param name="token">Refresh Token a ser validado e renovado.</param>
    /// <param name="ct">Token de cancelamento.</param>
    /// <returns>
    /// ErrorOr com ResultadoLogin (novos tokens + entidade Usuario) em caso de sucesso,
    /// ou Error se o Refresh Token for inválido/expirado/revogado.
    /// </returns>
    public async Task<ErrorOr<ResultadoLogin>> ExecutarAsync(
        string tokenBruto,
        CancellationToken ct)
    {
        // ---------------------------------------------------------------
        // Passo 1: Computar o hash do token recebido
        // ---------------------------------------------------------------
        // O cliente envia o token bruto (via cookie HttpOnly).
        // O banco armazena apenas o hash SHA256. Para buscar o token,
        // precisamos computar o hash do valor recebido e comparar.
        var tokenHash = RefreshToken.ComputarHash(tokenBruto);

        // ---------------------------------------------------------------
        // Passo 2: Buscar o usuário pelo hash do Refresh Token
        // ---------------------------------------------------------------
        // O repositório busca o usuário que possui o hash informado.
        // Se nenhum usuário for encontrado, o token não existe no banco.
        var usuario = await _usuarioRepository.ObterPorRefreshTokenHashAsync(tokenHash);

        if (usuario is null)
            return Erros.Auth.RefreshTokenInvalido;

        // ---------------------------------------------------------------
        // Passo 3: Localizar o token específico na lista do usuário
        // ---------------------------------------------------------------
        // O usuário pode ter vários refresh tokens (um por dispositivo/sessão).
        // Usamos LINQ Single() pois sabemos que o hash é único.
        var refreshTokenAtual = usuario.RefreshTokens
            .Single(rt => rt.TokenHash == tokenHash);

        // ---------------------------------------------------------------
        // Passo 4: Verificar se o token está ativo (Rich Domain Model)
        // ---------------------------------------------------------------
        // A propriedade Ativo da entidade RefreshToken encapsula a lógica:
        // o token está ativo quando NÃO expirou E NÃO foi revogado.
        if (!refreshTokenAtual.Ativo)
            return Erros.Auth.RefreshTokenInvalido;

        // ---------------------------------------------------------------
        // Passo 5: Verificar se o usuário ainda está ativo
        // ---------------------------------------------------------------
        if (!usuario.Ativo)
            return Erros.Usuario.Inativo;

        // ---------------------------------------------------------------
        // Passo 6: Revogar o token antigo (Rich Domain Model)
        // ---------------------------------------------------------------
        // O método Revogar() da entidade RefreshToken marca RevogadoEm
        // com a data/hora atual. Cada Refresh Token só pode ser usado UMA VEZ.
        refreshTokenAtual.Revogar();

        // ---------------------------------------------------------------
        // Passo 7: Gerar novos tokens
        // ---------------------------------------------------------------
        var novoAccessToken = _tokenService.GerarAccessToken(usuario);
        var (novoTokenBruto, novoRefreshToken) = _tokenService.GerarRefreshToken();
        novoRefreshToken.UsuarioId = usuario.Id;
        usuario.RefreshTokens.Add(novoRefreshToken);

        // ---------------------------------------------------------------
        // Passo 8: Salvar as alterações e retornar o resultado de domínio
        // ---------------------------------------------------------------
        // Duas operações persistidas atomicamente:
        // 1. UPDATE no token antigo (RevogadoEm = agora)
        // 2. INSERT do novo token (com hash)
        await _unitOfWork.SaveChangesAsync(ct);

        return new ResultadoLogin(
            AccessToken: novoAccessToken,
            RefreshToken: novoTokenBruto,   // Token bruto (para o cookie HttpOnly)
            ExpiraEm: novoRefreshToken.ExpiraEm,
            Usuario: usuario
        );
    }
}
