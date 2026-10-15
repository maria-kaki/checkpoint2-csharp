using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Domain;
using ExpenseHub.Api.Security;
using ExpenseHub.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace ExpenseHub.Api.Data;

/// <summary>
/// Implementação de <see cref="IExpenseRepository"/> com Entity Framework Core.
/// </summary>
public sealed class EfExpenseRepository : IExpenseRepository
{
    private readonly AppDbContext _dbContext;

    /// <summary>
    /// Cria o repositório.
    /// </summary>
    /// <param name="dbContext">Contexto do banco.</param>
    public EfExpenseRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public Task<ExpenseCategory?> FindCategoryAsync(int categoryId, CancellationToken cancellationToken)
    {
        return _dbContext.ExpenseCategories
            .FirstOrDefaultAsync(category => category.Id == categoryId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<Expense?> FindVisibleAsync(Guid id, CurrentUser user, CancellationToken cancellationToken)
    {
        return ExpenseVisibility.Apply(_dbContext.Expenses, user)
            .Include(expense => expense.Category)
            .FirstOrDefaultAsync(expense => expense.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public Task<Expense?> FindForTransitionAsync(Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.Expenses
            .Include(expense => expense.Category)
            .FirstOrDefaultAsync(expense => expense.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Expense>> ListVisibleAsync(CurrentUser user, CancellationToken cancellationToken)
    {
        return await ExpenseVisibility.Apply(_dbContext.Expenses.AsNoTracking(), user)
            .Include(expense => expense.Category)
            .OrderByDescending(expense => expense.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public void Add(Expense expense)
    {
        _dbContext.Expenses.Add(expense);
    }

    /// <inheritdoc />
    public async Task<bool> SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
    }
}
