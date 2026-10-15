using System;

namespace ExpenseHub.Api.Domain;

/// <summary>
/// Registro do pagamento simulado de um reembolso aprovado.
/// </summary>
public class PaymentRecord
{
    private PaymentRecord()
    {
    }

    /// <summary>Identificador do pagamento.</summary>
    public Guid Id { get; private set; }

    /// <summary>Reembolso pago.</summary>
    public Guid ExpenseId { get; private set; }

    /// <summary>Usuário do financeiro que registrou o pagamento.</summary>
    public string PaidById { get; private set; } = string.Empty;

    /// <summary>Instante do pagamento em UTC, definido pelo servidor.</summary>
    public DateTime PaidAtUtc { get; private set; }

    /// <summary>Valor pago.</summary>
    public decimal Amount { get; private set; }

    /// <summary>Cria o registro de pagamento com dados derivados do reembolso e do servidor.</summary>
    /// <param name="expenseId">Reembolso pago.</param>
    /// <param name="paidById">Usuário do financeiro que registrou o pagamento.</param>
    /// <param name="paidAtUtc">Instante do pagamento em UTC.</param>
    /// <param name="amount">Valor integral do reembolso.</param>
    /// <returns>O pagamento criado.</returns>
    public static PaymentRecord Create(Guid expenseId, string paidById, DateTime paidAtUtc, decimal amount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(paidById);

        return new PaymentRecord
        {
            Id = Guid.CreateVersion7(),
            ExpenseId = expenseId,
            PaidById = paidById,
            PaidAtUtc = paidAtUtc,
            Amount = amount,
        };
    }
}
