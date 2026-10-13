using System;

namespace ExpenseHub.Api.Domain;

/// <summary>
/// Registro do pagamento simulado de um reembolso aprovado.
/// </summary>
public class PaymentRecord
{
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
}
