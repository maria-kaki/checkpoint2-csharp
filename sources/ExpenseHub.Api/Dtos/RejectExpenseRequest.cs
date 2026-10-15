using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using ExpenseHub.Api.Domain;

namespace ExpenseHub.Api.Dtos;

/// <summary>Dados aceitos para reprovar um reembolso.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class RejectExpenseRequest
{
    /// <summary>Motivo da reprovação, entre 10 e 500 caracteres.</summary>
    [Required]
    [StringLength(ExpenseHistory.JustificationMaxLength, MinimumLength = ExpenseHistory.JustificationMinLength)]
    public string? Justification { get; init; }
}
