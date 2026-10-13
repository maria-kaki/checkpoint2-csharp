namespace ExpenseHub.Api.Domain;

/// <summary>
/// Ações registradas no histórico de um reembolso.
/// </summary>
public enum ExpenseAction
{
    /// <summary>Criação do rascunho.</summary>
    Created = 0,

    /// <summary>Edição do rascunho.</summary>
    Updated = 1,

    /// <summary>Envio para aprovação.</summary>
    Submitted = 2,

    /// <summary>Aprovação.</summary>
    Approved = 3,

    /// <summary>Reprovação com justificativa.</summary>
    Rejected = 4,

    /// <summary>Registro de pagamento.</summary>
    Paid = 5,
}
