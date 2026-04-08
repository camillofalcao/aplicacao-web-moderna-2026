// ============================================================================
// Arquivo: Interfaces/IUnitOfWork.cs
// Descrição: Interface que define o contrato do padrão Unit of Work.
//
// CONCEITO IMPORTANTE (Unit of Work):
// O padrão Unit of Work garante que todas as operações de uma transação
// sejam salvas juntas (commit) ou nenhuma seja salva (rollback).
// No EF Core, o DbContext já implementa esse padrão internamente,
// mas criamos esta interface para:
// 1. Manter a abstração na camada de domínio
// 2. Permitir que os handlers chamem SaveChanges de forma explícita
// 3. Facilitar testes unitários
// ============================================================================

namespace Agendamentos.Entities.Interfaces;

/// <summary>
/// Contrato para o padrão Unit of Work.
/// Garante que todas as alterações pendentes sejam salvas atomicamente.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Salva todas as alterações pendentes no banco de dados.
    /// Retorna o número de registros afetados.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
