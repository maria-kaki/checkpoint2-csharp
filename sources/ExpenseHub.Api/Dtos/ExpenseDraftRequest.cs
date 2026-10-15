using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using ExpenseHub.Api.Domain;

namespace ExpenseHub.Api.Dtos;

/// <summary>
/// Dados aceitos para criar ou editar um rascunho. Proprietário, estado, ator e horários não fazem
/// parte do contrato: qualquer campo extra é rejeitado com 400.
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class ExpenseDraftRequest
{
    /// <summary>Categoria da despesa.</summary>
    [Required]
    [Range(1, int.MaxValue)]
    public int? CategoryId { get; init; }

    /// <summary>Descrição, entre 10 e 500 caracteres.</summary>
    [Required]
    [StringLength(Expense.DescriptionMaxLength, MinimumLength = Expense.DescriptionMinLength)]
    public string? Description { get; init; }

    /// <summary>Valor entre R$ 0,01 e R$ 2.147.483.647,00.</summary>
    [Required]
    [Range(
        typeof(decimal),
        ExpenseRules.MinAmountText,
        ExpenseRules.MaxAmountText,
        ParseLimitsInInvariantCulture = true,
        ConvertValueInInvariantCulture = true)]
    public decimal? Amount { get; init; }

    /// <summary>Data da despesa (não pode ser futura).</summary>
    [Required]
    public DateOnly? ExpenseDate { get; init; }
}
