using System;
using System.Linq;
using ExpenseHub.Api.Domain;
using ExpenseHub.Api.Security;

namespace ExpenseHub.Api.Services;

/// <summary>
/// Escopo de leitura de reembolsos por perfil, aplicado na consulta antes de materializar os dados.
/// Roles acumuladas recebem a união dos escopos.
/// </summary>
public static class ExpenseVisibility
{
    /// <summary>
    /// Restringe a consulta aos reembolsos visíveis para o usuário:
    /// Employee vê os próprios, Approver vê Submitted, Finance vê Approved e Paid e Auditor vê todos.
    /// Admin não recebe acesso apenas por ser Admin.
    /// </summary>
    /// <param name="query">Consulta de reembolsos.</param>
    /// <param name="user">Usuário autenticado.</param>
    /// <returns>A consulta filtrada.</returns>
    public static IQueryable<Expense> Apply(IQueryable<Expense> query, CurrentUser user)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(user);

        if (user.IsInRole(AppRoles.Auditor))
        {
            return query;
        }

        string userId = user.Id;
        bool isEmployee = user.IsInRole(AppRoles.Employee);
        bool isApprover = user.IsInRole(AppRoles.Approver);
        bool isFinance = user.IsInRole(AppRoles.Finance);

        return query.Where(expense =>
            (isEmployee && expense.OwnerId == userId)
            || (isApprover && expense.Status == ExpenseStatus.Submitted)
            || (isFinance && (expense.Status == ExpenseStatus.Approved || expense.Status == ExpenseStatus.Paid)));
    }
}
