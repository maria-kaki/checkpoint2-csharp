using System;
using ExpenseHub.Api.Domain;

namespace ExpenseHub.Api.Responses;

/// <summary>Registro auditável de uma ação sobre um reembolso.</summary>
/// <param name="Id">Identificador do registro.</param>
/// <param name="Action">Ação executada.</param>
/// <param name="ActorId">Usuário que executou a ação.</param>
/// <param name="OccurredAtUtc">Instante da ação em UTC.</param>
/// <param name="PreviousStatus">Estado anterior.</param>
/// <param name="NewStatus">Estado posterior.</param>
/// <param name="Justification">Justificativa da reprovação.</param>
/// <param name="Changes">Alterações realizadas no rascunho.</param>
public sealed record ExpenseHistoryResponse(
    Guid Id,
    ExpenseAction Action,
    string ActorId,
    DateTime OccurredAtUtc,
    ExpenseStatus? PreviousStatus,
    ExpenseStatus NewStatus,
    string? Justification,
    string? Changes)
{
    /// <summary>Converte a entidade em resposta.</summary>
    /// <param name="history">Registro persistido.</param>
    /// <returns>A resposta.</returns>
    public static ExpenseHistoryResponse FromEntity(ExpenseHistory history)
    {
        ArgumentNullException.ThrowIfNull(history);

        return new ExpenseHistoryResponse(
            history.Id,
            history.Action,
            history.ActorId,
            history.OccurredAtUtc,
            history.PreviousStatus,
            history.NewStatus,
            history.Justification,
            history.Changes);
    }
}
