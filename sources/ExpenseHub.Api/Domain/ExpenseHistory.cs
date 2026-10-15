using System;

namespace ExpenseHub.Api.Domain;

/// <summary>
/// Registro imutável de uma ação executada sobre um reembolso.
/// </summary>
public class ExpenseHistory
{
    /// <summary>Tamanho mínimo da justificativa.</summary>
    public const int JustificationMinLength = 10;

    /// <summary>Tamanho máximo da justificativa.</summary>
    public const int JustificationMaxLength = 500;

    /// <summary>Tamanho máximo da descrição das alterações.</summary>
    public const int ChangesMaxLength = 2000;

    private ExpenseHistory()
    {
    }

    /// <summary>Identificador do registro.</summary>
    public Guid Id { get; private set; }

    /// <summary>Reembolso relacionado.</summary>
    public Guid ExpenseId { get; private set; }

    /// <summary>Ação executada.</summary>
    public ExpenseAction Action { get; private set; }

    /// <summary>Usuário que executou a ação.</summary>
    public string ActorId { get; private set; } = string.Empty;

    /// <summary>Instante da ação em UTC, definido pelo servidor.</summary>
    public DateTime OccurredAtUtc { get; private set; }

    /// <summary>Estado anterior; nulo na criação.</summary>
    public ExpenseStatus? PreviousStatus { get; private set; }

    /// <summary>Estado posterior à ação.</summary>
    public ExpenseStatus NewStatus { get; private set; }

    /// <summary>Justificativa da reprovação.</summary>
    public string? Justification { get; private set; }

    /// <summary>Alterações realizadas enquanto o reembolso estava em Draft.</summary>
    public string? Changes { get; private set; }

    /// <summary>
    /// Cria um registro de histórico. Ator e instante devem vir da identidade autenticada e do servidor.
    /// </summary>
    /// <param name="expenseId">Reembolso relacionado.</param>
    /// <param name="action">Ação executada.</param>
    /// <param name="actorId">Usuário que executou a ação.</param>
    /// <param name="occurredAtUtc">Instante da ação em UTC.</param>
    /// <param name="previousStatus">Estado anterior; nulo na criação.</param>
    /// <param name="newStatus">Estado posterior.</param>
    /// <param name="justification">Justificativa, quando houver.</param>
    /// <param name="changes">Descrição das alterações, quando houver.</param>
    /// <returns>O registro criado.</returns>
    public static ExpenseHistory Create(
        Guid expenseId,
        ExpenseAction action,
        string actorId,
        DateTime occurredAtUtc,
        ExpenseStatus? previousStatus,
        ExpenseStatus newStatus,
        string? justification = null,
        string? changes = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);

        return new ExpenseHistory
        {
            Id = Guid.CreateVersion7(),
            ExpenseId = expenseId,
            Action = action,
            ActorId = actorId,
            OccurredAtUtc = occurredAtUtc,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            Justification = justification,
            Changes = changes,
        };
    }
}
