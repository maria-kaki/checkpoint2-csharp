using System;

namespace ExpenseHub.Api.Domain;

/// <summary>
/// Registro imutável de uma ação executada sobre um reembolso.
/// </summary>
public class ExpenseHistory
{
    /// <summary>Tamanho máximo da justificativa.</summary>
    public const int JustificationMaxLength = 500;

    /// <summary>Tamanho máximo da descrição das alterações.</summary>
    public const int ChangesMaxLength = 2000;

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
}
