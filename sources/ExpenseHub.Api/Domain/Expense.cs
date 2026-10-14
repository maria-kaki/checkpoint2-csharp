using System;
using System.Collections.Generic;

namespace ExpenseHub.Api.Domain;

/// <summary>
/// Solicitação de reembolso com um único valor.
/// </summary>
public class Expense
{
    /// <summary>Tamanho mínimo da descrição.</summary>
    public const int DescriptionMinLength = 10;

    /// <summary>Tamanho máximo da descrição.</summary>
    public const int DescriptionMaxLength = 500;

    /// <summary>Identificador gerado pelo servidor.</summary>
    public Guid Id { get; private set; }

    /// <summary>Identificador do usuário proprietário, obtido do token.</summary>
    public string OwnerId { get; private set; } = string.Empty;

    /// <summary>Identificador da categoria.</summary>
    public int CategoryId { get; private set; }

    /// <summary>Categoria da despesa.</summary>
    public ExpenseCategory? Category { get; private set; }

    /// <summary>Descrição da despesa.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Valor solicitado em reais.</summary>
    public decimal Amount { get; private set; }

    /// <summary>Data em que a despesa ocorreu.</summary>
    public DateOnly ExpenseDate { get; private set; }

    /// <summary>Estado atual, controlado pelo servidor.</summary>
    public ExpenseStatus Status { get; private set; }

    /// <summary>Instante de criação em UTC.</summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>Instante da última alteração em UTC.</summary>
    public DateTime UpdatedAtUtc { get; private set; }

    /// <summary>Token de concorrência otimista, renovado a cada alteração.</summary>
    public Guid ConcurrencyStamp { get; private set; }

    /// <summary>Registro de pagamento, quando pago.</summary>
    public PaymentRecord? Payment { get; private set; }

    /// <summary>Histórico de ações do reembolso.</summary>
    public ICollection<ExpenseHistory> History { get; } = [];
}
