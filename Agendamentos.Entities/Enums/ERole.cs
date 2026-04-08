// ============================================================================
// Arquivo: Enums/Role.cs
// Descrição: Enum que define os papéis (roles) dos usuários no sistema.
//            Utilizado para controle de acesso baseado em roles (RBAC).
// ============================================================================

namespace Agendamentos.Entities.Enums;

/// <summary>
/// Representa os diferentes papéis que um usuário pode ter no sistema.
/// Cada role define um nível de permissão diferente.
/// </summary>
public enum ERole
{
    /// <summary>
    /// Usuário comum que pode se cadastrar, fazer login e agendar serviços.
    /// É o papel padrão atribuído no momento do cadastro.
    /// </summary>
    Cliente = 0,

    /// <summary>
    /// Prestador de serviços que possui uma agenda própria.
    /// Só pode ser criado pelo Administrador.
    /// Pode gerenciar seus horários disponíveis e visualizar seus agendamentos.
    /// </summary>
    Prestador = 1,

    /// <summary>
    /// Administrador do sistema com acesso total.
    /// Pode criar prestadores de serviços e gerenciar todos os recursos.
    /// Um usuário admin é criado automaticamente no seed do banco de dados.
    /// </summary>
    Admin = 2
}
