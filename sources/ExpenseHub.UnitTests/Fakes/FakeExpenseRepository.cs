using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Domain;
using ExpenseHub.Api.Security;
using ExpenseHub.Api.Services;

namespace ExpenseHub.UnitTests.Fakes;

/// <summary>
/// Repositório em memória. Aplica o mesmo filtro de visibilidade da implementação real.
/// </summary>
internal sealed class FakeExpenseRepository : IExpenseRepository
{
    private readonly List<Expense> _expenses = [];

    private readonly List<ExpenseCategory> _categories =
    [
        new(1, "Alimentação"),
        new(2, "Transporte"),
        new(3, "Hospedagem"),
    ];

    public IReadOnlyList<Expense> Expenses => _expenses;

    public int SaveCount { get; private set; }

    public bool SimulateConcurrencyConflict { get; set; }

    public ExpenseCategory Category(int id)
    {
        return _categories.Single(category => category.Id == id);
    }

    public Expense Seed(Expense expense)
    {
        _expenses.Add(expense);
        return expense;
    }

    public Task<ExpenseCategory?> FindCategoryAsync(int categoryId, CancellationToken cancellationToken)
    {
        return Task.FromResult(_categories.FirstOrDefault(category => category.Id == categoryId));
    }

    public Task<Expense?> FindVisibleAsync(Guid id, CurrentUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(ExpenseVisibility.Apply(_expenses.AsQueryable(), user)
            .FirstOrDefault(expense => expense.Id == id));
    }

    public Task<Expense?> FindForTransitionAsync(Guid id, CancellationToken cancellationToken)
    {
        return Task.FromResult(_expenses.FirstOrDefault(expense => expense.Id == id));
    }

    public Task<Expense?> FindVisibleWithHistoryAsync(
        Guid id,
        CurrentUser user,
        CancellationToken cancellationToken)
    {
        return FindVisibleAsync(id, user, cancellationToken);
    }

    public Task<IReadOnlyList<Expense>> ListVisibleAsync(CurrentUser user, CancellationToken cancellationToken)
    {
        IReadOnlyList<Expense> visible = [.. ExpenseVisibility.Apply(_expenses.AsQueryable(), user)
            .OrderByDescending(expense => expense.CreatedAtUtc)];
        return Task.FromResult(visible);
    }

    public void Add(Expense expense)
    {
        _expenses.Add(expense);
    }

    public Task<bool> SaveChangesAsync(CancellationToken cancellationToken)
    {
        if (SimulateConcurrencyConflict)
        {
            return Task.FromResult(false);
        }

        SaveCount++;
        return Task.FromResult(true);
    }
}
