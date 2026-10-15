using System;
using ExpenseHub.Api.Domain;

namespace ExpenseHub.Api.Responses;

/// <summary>
/// Reembolso retornado pela API.
/// </summary>
/// <param name="Id">Identificador.</param>
/// <param name="OwnerId">Proprietário.</param>
/// <param name="CategoryId">Categoria.</param>
/// <param name="CategoryName">Nome da categoria.</param>
/// <param name="Description">Descrição.</param>
/// <param name="Amount">Valor.</param>
/// <param name="ExpenseDate">Data da despesa.</param>
/// <param name="Status">Estado atual.</param>
/// <param name="CreatedAtUtc">Instante de criação em UTC.</param>
/// <param name="UpdatedAtUtc">Instante da última alteração em UTC.</param>
public sealed record ExpenseResponse(
    Guid Id,
    string OwnerId,
    int CategoryId,
    string CategoryName,
    string Description,
    decimal Amount,
    DateOnly ExpenseDate,
    ExpenseStatus Status,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc)
{
    /// <summary>
    /// Converte a entidade em resposta.
    /// </summary>
    /// <param name="expense">Reembolso com a categoria carregada.</param>
    /// <returns>A resposta.</returns>
    public static ExpenseResponse FromEntity(Expense expense)
    {
        ArgumentNullException.ThrowIfNull(expense);

        return new ExpenseResponse(
            expense.Id,
            expense.OwnerId,
            expense.CategoryId,
            expense.Category?.Name ?? string.Empty,
            expense.Description,
            expense.Amount,
            expense.ExpenseDate,
            expense.Status,
            expense.CreatedAtUtc,
            expense.UpdatedAtUtc);
    }
}
