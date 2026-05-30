// ============================================================================
// Arquivo: UseCases/Usuarios/LoginUseCase.cs
// Descrição: Caso de uso para autenticação (login) de usuários.
//
// CONCEITO IMPORTANTE (Fluxo de Autenticação JWT):
// 1. Cliente envia e-mail + senha
// 2. Servidor verifica credenciais no banco de dados
// 3. Se válidas, gera um Access Token (JWT) + Refresh Token
// 4. Cliente armazena os tokens e os envia em requisições futuras
// 5. Access Token expira → cliente usa Refresh Token para obter novos tokens
//
// CONCEITO IMPORTANTE (Segurança no Login):
// - A mensagem de erro para credenciais inválidas é genérica ("E-mail ou senha
//   inválidos") propositalmente. Se disséssemos "e-mail não encontrado" ou
//   "senha incorreta" separadamente, um atacante poderia enumerar e-mails válidos.
// - Verificamos se o usuário está ativo APÓS validar a senha, pois um usuário
//   desativado não deve saber se sua senha está correta ou não.
//
// CONCEITO IMPORTANTE (Rich Domain Model):
// A verificação da senha é feita pelo método VerificarSenha() da entidade
// Usuario, encapsulando a lógica de comparação BCrypt no domínio.
// ============================================================================

using Agendamentos.Entities.Interfaces;
using Agendamentos.Entities.Models;
using Agendamentos.Entities.UseCases.Common;
using ErrorOr;

namespace Agendamentos.Entities.UseCases.Usuarios;

/// <summary>
/// Caso de uso responsável por autenticar um usuário (login).
///
/// Fluxo de execução:
/// 1. Busca o usuário pelo e-mail
/// 2. Verifica a senha usando o método da entidade (VerificarSenha)
/// 3. Verifica se o usuário está ativo
/// 4. Gera Access Token (JWT) + Refresh Token
/// 5. Salva o Refresh Token no banco de dados
/// 6. Retorna o ResultadoLogin com tokens + dados do usuário
/// </summary>
public class LoginUseCase
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly ITokenService _tokenService;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Construtor com injeção de dependência.
    /// </summary>
    /// <param name="usuarioRepository">Repositório para buscar o usuário pelo e-mail.</param>
    /// <param name="tokenService">Serviço de geração de tokens JWT e Refresh.</param>
    /// <param name="unitOfWork">Unit of Work para persistir o Refresh Token.</param>
    public LoginUseCase(
        IUsuarioRepository usuarioRepository,
        ITokenService tokenService,
        IUnitOfWork unitOfWork)
    {
        _usuarioRepository = usuarioRepository;
        _tokenService = tokenService;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Executa a autenticação do usuário.
    ///
    /// Este caso de uso recebe e-mail e senha diretamente (não uma entidade),
    /// pois o login é uma operação de autenticação que não cria nem modifica
    /// uma entidade — apenas verifica credenciais e gera tokens.
    /// </summary>
    /// <param name="email">E-mail do usuário.</param>
    /// <param name="senha">Senha em texto plano para verificação.</param>
    /// <param name="ct">Token de cancelamento.</param>
    /// <returns>
    /// ErrorOr com ResultadoLogin (tokens + entidade Usuario) em caso de sucesso,
    /// ou Error em caso de falha (credenciais inválidas, usuário inativo).
    /// </returns>
    public async Task<ErrorOr<ResultadoLogin>> ExecutarAsync(
        string email,
        string senha,
        CancellationToken ct)
    {
        // ---------------------------------------------------------------
        // Passo 1: Buscar o usuário pelo e-mail
        // ---------------------------------------------------------------
        // Se o e-mail não existir no banco, retornamos erro genérico.
        // Note que usamos a MESMA mensagem de erro para e-mail inexistente
        // e senha incorreta, evitando enumeração de e-mails por atacantes.
        var usuario = await _usuarioRepository.ObterPorEmailAsync(email);

        if (usuario is null)
            return Erros.Usuario.SenhaInvalida;

        // ---------------------------------------------------------------
        // Passo 2: Verificar a senha usando Rich Domain Model
        // ---------------------------------------------------------------
        // A entidade Usuario encapsula a lógica de verificação BCrypt
        // no método VerificarSenha(). Internamente, ele extrai o salt
        // do hash armazenado, aplica o mesmo algoritmo na senha fornecida
        // e compara os resultados.
        if (!usuario.VerificarSenha(senha))
            return Erros.Usuario.SenhaInvalida;

        // ---------------------------------------------------------------
        // Passo 3: Verificar se o usuário está ativo
        // ---------------------------------------------------------------
        // Um administrador pode desativar um usuário sem excluí-lo (soft delete).
        // Verificamos APÓS validar a senha para não dar pistas sobre o status.
        if (!usuario.Ativo)
            return Erros.Usuario.Inativo;

        // ---------------------------------------------------------------
        // Passo 4: Gerar o Access Token (JWT)
        // ---------------------------------------------------------------
        // O ITokenService.GerarAccessToken cria um JWT contendo as claims:
        // - sub (subject): Id do usuário
        // - email: e-mail do usuário
        // - role: papel do usuário (Cliente, Prestador, Admin)
        var accessToken = _tokenService.GerarAccessToken(usuario);

        // ---------------------------------------------------------------
        // Passo 5: Gerar o Refresh Token
        // ---------------------------------------------------------------
        // O Refresh Token é uma string aleatória criptograficamente segura.
        // Diferente do JWT, ele não contém informações — é apenas um
        // identificador único que referencia o registro no banco de dados.
        //
        // GerarRefreshToken retorna uma tupla com:
        // - tokenBruto: o valor original (será enviado ao cliente via cookie)
        // - entidade: a entidade com o hash SHA256 (será salva no banco)
        //
        // O banco NUNCA armazena o token bruto — apenas o hash. Assim,
        // mesmo que o banco seja comprometido, os tokens não podem ser usados.
        var (tokenBruto, refreshToken) = _tokenService.GerarRefreshToken();
        refreshToken.UsuarioId = usuario.Id;
        usuario.RefreshTokens.Add(refreshToken);

        // ---------------------------------------------------------------
        // Passo 6: Salvar no banco e retornar o resultado de domínio
        // ---------------------------------------------------------------
        await _unitOfWork.SaveChangesAsync(ct);

        return new ResultadoLogin(
            AccessToken: accessToken,
            RefreshToken: tokenBruto,   // Token bruto (para o cookie HttpOnly)
            ExpiraEm: refreshToken.ExpiraEm,
            Usuario: usuario
        );
    }
}
