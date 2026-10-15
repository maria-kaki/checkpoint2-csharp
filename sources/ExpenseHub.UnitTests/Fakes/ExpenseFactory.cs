using System;
using ExpenseHub.Api.Domain;
using ExpenseHub.Api.Dtos;

namespace ExpenseHub.UnitTests.Fakes;

/// <summary>
/// Dados válidos de reembolso para os testes.
/// </summary>
internal static class ExpenseFactory
{
    public const string ValidDescription = "Táxi do aeroporto até o cliente";

    public static Expense Draft(string ownerId, ExpenseCategory category, FixedTimeProvider clock)
    {
        return Expense.Create(ownerId, category, ValidDescription, 87.50m, clock.Today, clock.UtcNow);
    }

    public static ExpenseDraftRequest Request(
        int categoryId = 2,
        string description = ValidDescription,
        decimal amount = 87.50m,
        DateOnly? expenseDate = null)
    {
        return new ExpenseDraftRequest
        {
            CategoryId = categoryId,
            Description = description,
            Amount = amount,
            ExpenseDate = expenseDate ?? DateOnly.FromDateTime(FixedTimeProvider.DefaultNow.UtcDateTime),
        };
    }
}
