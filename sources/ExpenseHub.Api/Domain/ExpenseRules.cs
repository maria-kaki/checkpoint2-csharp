using System;
using System.Collections.Generic;
using System.Globalization;

namespace ExpenseHub.Api.Domain;

/// <summary>
/// Regras de validação dos dados de um reembolso, compartilhadas pelo domínio e pelos serviços.
/// </summary>
public static class ExpenseRules
{
    /// <summary>Valor mínimo, em texto invariável, para uso em atributos de validação.</summary>
    public const string MinAmountText = "0.01";

    /// <summary>Valor máximo (<see cref="int.MaxValue"/>), em texto invariável, para uso em atributos de validação.</summary>
    public const string MaxAmountText = "2147483647";

    /// <summary>Valor mínimo aceito.</summary>
    public static readonly decimal MinAmount = 0.01m;

    /// <summary>Valor máximo aceito.</summary>
    public static readonly decimal MaxAmount = int.MaxValue;

    /// <summary>
    /// Valida descrição, valor e data da despesa.
    /// </summary>
    /// <param name="description">Descrição informada.</param>
    /// <param name="amount">Valor informado.</param>
    /// <param name="expenseDate">Data da despesa.</param>
    /// <param name="today">Data atual do servidor.</param>
    /// <returns>Erros por campo; vazio quando os dados são válidos.</returns>
    public static IReadOnlyDictionary<string, string[]> Validate(
        string? description,
        decimal amount,
        DateOnly expenseDate,
        DateOnly today)
    {
        Dictionary<string, string[]> errors = [];

        int descriptionLength = description?.Trim().Length ?? 0;
        if (descriptionLength < Expense.DescriptionMinLength || descriptionLength > Expense.DescriptionMaxLength)
        {
            errors["description"] =
            [
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"A descrição deve ter entre {Expense.DescriptionMinLength} e {Expense.DescriptionMaxLength} caracteres."),
            ];
        }

        if (amount < MinAmount || amount > MaxAmount)
        {
            errors["amount"] = ["O valor deve estar entre R$ 0,01 e R$ 2.147.483.647,00."];
        }

        if (expenseDate > today)
        {
            errors["expenseDate"] = ["A data da despesa não pode ser futura."];
        }

        return errors;
    }
}
