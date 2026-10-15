using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Domain;
using ExpenseHub.Api.Security;

namespace ExpenseHub.Api.Services;

/// <summary>
/// Persistência de reembolsos usada pelos serviços. A abstração permite testar as regras sem banco.
/// </summary>
public interface IExpenseRepository
{
    /// <summary>
    /// Obtém uma categoria pelo identificador.
    /// </summary>
    /// <param name="categoryId">Identificador da categoria.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A categoria ou <see langword="null"/>.</returns>
    Task<ExpenseCategory?> FindCategoryAsync(int categoryId, CancellationToken cancellationToken);

    /// <summary>
    /// Obtém um reembolso, com a categoria, somente se estiver no escopo de leitura do usuário.
    /// </summary>
    /// <param name="id">Identificador do reembolso.</param>
    /// <param name="user">Usuário autenticado.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O reembolso ou <see langword="null"/> quando inexistente ou invisível.</returns>
    Task<Expense?> FindVisibleAsync(Guid id, CurrentUser user, CancellationToken cancellationToken);

    /// <summary>
    /// Obtém um reembolso para validar uma transição. O serviço deve validar a role antes da consulta e
    /// aplicar ownership e estado antes de alterar o agregado.
    /// </summary>
    /// <param name="id">Identificador do reembolso.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O reembolso ou <see langword="null"/> quando inexistente.</returns>
    Task<Expense?> FindForTransitionAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Lista, com a categoria, os reembolsos no escopo de leitura do usuário. O filtro é aplicado na
    /// consulta, antes de materializar os dados.
    /// </summary>
    /// <param name="user">Usuário autenticado.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Reembolsos visíveis, do mais recente para o mais antigo.</returns>
    Task<IReadOnlyList<Expense>> ListVisibleAsync(CurrentUser user, CancellationToken cancellationToken);

    /// <summary>
    /// Inclui um novo reembolso para ser persistido.
    /// </summary>
    /// <param name="expense">Reembolso criado.</param>
    void Add(Expense expense);

    /// <summary>
    /// Persiste a alteração e o histórico na mesma operação.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><see langword="false"/> quando outra operação alterou o reembolso ao mesmo tempo.</returns>
    Task<bool> SaveChangesAsync(CancellationToken cancellationToken);
}
