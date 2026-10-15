using System.Linq;
using ExpenseHub.Api.Domain;
using ExpenseHub.Api.Security;
using ExpenseHub.Api.Services;
using ExpenseHub.UnitTests.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Services;

/// <summary>
/// Escopo de leitura por perfil aplicado à consulta.
/// </summary>
[TestClass]
public sealed class ExpenseVisibilityTests
{
    private static readonly ExpenseCategory _transport = new(2, "Transporte");

    private readonly FixedTimeProvider _clock = new();

    /// <summary>Employee enxerga apenas os próprios reembolsos.</summary>
    [TestMethod]
    public void Apply_Employee_VeApenasOsProprios()
    {
        IQueryable<Expense> expenses = Expenses("ana", "bruno", "ana");

        Expense[] visible = [.. ExpenseVisibility.Apply(expenses, TestUsers.Employee("ana"))];

        Assert.HasCount(2, visible);
        Assert.IsTrue(visible.All(expense => expense.OwnerId == "ana"));
    }

    /// <summary>Auditor enxerga todos os reembolsos.</summary>
    [TestMethod]
    public void Apply_Auditor_VeTodos()
    {
        IQueryable<Expense> expenses = Expenses("ana", "bruno");

        Assert.AreEqual(2, ExpenseVisibility.Apply(expenses, TestUsers.With("auditor", AppRoles.Auditor)).Count());
    }

    /// <summary>Admin sem outra role não recebe acesso funcional aos reembolsos.</summary>
    [TestMethod]
    public void Apply_SomenteAdmin_NaoVeNada()
    {
        IQueryable<Expense> expenses = Expenses("ana", "bruno");

        Assert.AreEqual(0, ExpenseVisibility.Apply(expenses, TestUsers.With("admin", AppRoles.Admin)).Count());
    }

    /// <summary>Usuário autenticado sem roles não enxerga nada.</summary>
    [TestMethod]
    public void Apply_SemRoles_NaoVeNada()
    {
        IQueryable<Expense> expenses = Expenses("ana");

        Assert.AreEqual(0, ExpenseVisibility.Apply(expenses, TestUsers.With("carla")).Count());
    }

    /// <summary>Approver não enxerga rascunhos de outras pessoas.</summary>
    [TestMethod]
    public void Apply_Approver_NaoVeDraftDeOutros()
    {
        IQueryable<Expense> expenses = Expenses("ana");

        Assert.AreEqual(0, ExpenseVisibility.Apply(expenses, TestUsers.With("duda", AppRoles.Approver)).Count());
    }

    private IQueryable<Expense> Expenses(params string[] owners)
    {
        return owners
            .Select(owner => ExpenseFactory.Draft(owner, _transport, _clock))
            .ToList()
            .AsQueryable();
    }
}
