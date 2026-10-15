using System;
using ExpenseHub.Api.Domain;
using ExpenseHub.Api.Security;

namespace ExpenseHub.Api.Services;

/// <summary>
/// Regras contextuais de escrita que combinam role, proprietário e estado. Os controllers restringem
/// a rota, enquanto esta camada impede autoaprovação e autopagamento mesmo quando roles se acumulam.
/// </summary>
public static class ExpenseAccess
{
    /// <summary>Verifica se alguma role do usuário concede escopo de leitura de reembolsos.</summary>
    /// <param name="user">Usuário autenticado.</param>
    /// <returns><see langword="true"/> para Employee, Approver, Finance ou Auditor.</returns>
    public static bool CanRead(CurrentUser user)
    {
        ArgumentNullException.ThrowIfNull(user);

        return user.IsInRole(AppRoles.Employee)
            || user.IsInRole(AppRoles.Approver)
            || user.IsInRole(AppRoles.Finance)
            || user.IsInRole(AppRoles.Auditor);
    }

    /// <summary>Valida se o usuário pode aprovar ou reprovar o reembolso.</summary>
    /// <param name="user">Usuário autenticado.</param>
    /// <param name="expense">Reembolso localizado para a transição.</param>
    /// <returns>Nulo quando a decisão é permitida; caso contrário, a falha correspondente.</returns>
    public static ServiceError? ValidateDecision(CurrentUser user, Expense expense)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(expense);

        if (!user.IsInRole(AppRoles.Approver))
        {
            return ServiceError.Forbidden("Somente usuários com a role Approver decidem reembolsos.");
        }

        if (string.Equals(expense.OwnerId, user.Id, StringComparison.Ordinal))
        {
            return ServiceError.Forbidden("O proprietário não pode decidir o próprio reembolso.");
        }

        return expense.Status == ExpenseStatus.Submitted
            ? null
            : ServiceError.Conflict($"Somente reembolsos em Submitted podem ser decididos. Estado atual: {expense.Status}.");
    }

    /// <summary>Valida se o usuário pode registrar o pagamento do reembolso.</summary>
    /// <param name="user">Usuário autenticado.</param>
    /// <param name="expense">Reembolso visível ao usuário.</param>
    /// <returns>Nulo quando o pagamento é permitido; caso contrário, a falha correspondente.</returns>
    public static ServiceError? ValidatePayment(CurrentUser user, Expense expense)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(expense);

        if (!user.IsInRole(AppRoles.Finance))
        {
            return ServiceError.Forbidden("Somente usuários com a role Finance registram pagamentos.");
        }

        if (string.Equals(expense.OwnerId, user.Id, StringComparison.Ordinal))
        {
            return ServiceError.Forbidden("O proprietário não pode pagar o próprio reembolso.");
        }

        return expense.Status == ExpenseStatus.Approved
            ? null
            : ServiceError.Conflict($"Somente reembolsos em Approved podem ser pagos. Estado atual: {expense.Status}.");
    }
}
