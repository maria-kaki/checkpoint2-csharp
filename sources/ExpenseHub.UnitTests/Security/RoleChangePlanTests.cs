using System;
using System.Collections.Generic;
using System.Linq;
using ExpenseHub.Api.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Security;

/// <summary>
/// Regras de alteração de roles pelo Admin.
/// </summary>
[TestClass]
public sealed class RoleChangePlanTests
{
    /// <summary>Roles válidas geram a diferença exata entre o conjunto atual e o desejado.</summary>
    [TestMethod]
    public void Create_ComRolesConhecidas_CalculaRolesAAdicionarERemover()
    {
        RoleChangePlan plan = RoleChangePlan.Create(
            [AppRoles.Employee, AppRoles.Auditor],
            [AppRoles.Employee, AppRoles.Approver],
            isSelf: false);

        Assert.IsEmpty(plan.UnknownRoles);
        Assert.IsFalse(plan.RemovesOwnAdminRole);
        Assert.AreEqual("Approver", Sorted(plan.RolesToAdd));
        Assert.AreEqual("Auditor", Sorted(plan.RolesToRemove));
        Assert.AreEqual("Approver,Employee", Sorted(plan.FinalRoles));
    }

    /// <summary>Role inexistente não é criada implicitamente e é reportada.</summary>
    [TestMethod]
    public void Create_ComRoleDesconhecida_ReportaRoleSemIncluirNoResultado()
    {
        RoleChangePlan plan = RoleChangePlan.Create([], [AppRoles.Employee, "SuperUser"], isSelf: false);

        Assert.AreEqual("SuperUser", Sorted(plan.UnknownRoles));
        Assert.AreEqual("Employee", Sorted(plan.FinalRoles));
    }

    /// <summary>Valores nulos ou vazios na lista são tratados como roles desconhecidas.</summary>
    [TestMethod]
    public void Create_ComRoleNulaOuVazia_TrataComoDesconhecida()
    {
        RoleChangePlan plan = RoleChangePlan.Create([], [null, " "], isSelf: false);

        Assert.HasCount(2, plan.UnknownRoles);
        Assert.IsEmpty(plan.FinalRoles);
    }

    /// <summary>Nomes de roles são aceitos sem diferenciar maiúsculas e retornados no formato canônico.</summary>
    [TestMethod]
    public void Create_ComCaixaDiferente_NormalizaParaNomeCanonico()
    {
        RoleChangePlan plan = RoleChangePlan.Create([], ["employee", "FINANCE"], isSelf: false);

        Assert.IsEmpty(plan.UnknownRoles);
        Assert.AreEqual("Employee,Finance", Sorted(plan.FinalRoles));
    }

    /// <summary>Roles repetidas na requisição são consideradas uma única vez.</summary>
    [TestMethod]
    public void Create_ComRolesRepetidas_AdicionaUmaUnicaVez()
    {
        RoleChangePlan plan = RoleChangePlan.Create([], [AppRoles.Employee, "Employee", "employee"], isSelf: false);

        Assert.AreEqual("Employee", Sorted(plan.RolesToAdd));
    }

    /// <summary>O Admin não pode remover a própria role Admin.</summary>
    [TestMethod]
    public void Create_AdminRemovendoPropriaRoleAdmin_SinalizaBloqueio()
    {
        RoleChangePlan plan = RoleChangePlan.Create([AppRoles.Admin], [AppRoles.Employee], isSelf: true);

        Assert.IsTrue(plan.RemovesOwnAdminRole);
    }

    /// <summary>Uma lista vazia enviada pelo próprio Admin também removeria a role Admin e é bloqueada.</summary>
    [TestMethod]
    public void Create_AdminEnviandoListaVaziaParaSiMesmo_SinalizaBloqueio()
    {
        RoleChangePlan plan = RoleChangePlan.Create([AppRoles.Admin, AppRoles.Auditor], [], isSelf: true);

        Assert.IsTrue(plan.RemovesOwnAdminRole);
    }

    /// <summary>O Admin pode acumular outras roles desde que mantenha a role Admin.</summary>
    [TestMethod]
    public void Create_AdminMantendoRoleAdmin_PermiteAdicionarOutrasRoles()
    {
        RoleChangePlan plan = RoleChangePlan.Create([AppRoles.Admin], [AppRoles.Admin, AppRoles.Auditor], isSelf: true);

        Assert.IsFalse(plan.RemovesOwnAdminRole);
        Assert.AreEqual("Auditor", Sorted(plan.RolesToAdd));
        Assert.IsEmpty(plan.RolesToRemove);
    }

    /// <summary>A proteção vale apenas para a própria conta: um Admin pode remover a role Admin de outro usuário.</summary>
    [TestMethod]
    public void Create_RemovendoRoleAdminDeOutroUsuario_Permite()
    {
        RoleChangePlan plan = RoleChangePlan.Create([AppRoles.Admin], [AppRoles.Employee], isSelf: false);

        Assert.IsFalse(plan.RemovesOwnAdminRole);
        Assert.AreEqual("Admin", Sorted(plan.RolesToRemove));
    }

    /// <summary>Quando o conjunto desejado é igual ao atual, nada precisa ser aplicado.</summary>
    [TestMethod]
    public void Create_SemDiferenca_NaoPossuiAlteracoes()
    {
        RoleChangePlan plan = RoleChangePlan.Create(
            [AppRoles.Employee, AppRoles.Approver],
            [AppRoles.Approver, AppRoles.Employee],
            isSelf: false);

        Assert.IsFalse(plan.HasChanges);
    }

    private static string Sorted(IEnumerable<string> roles)
    {
        return string.Join(",", roles.Order(StringComparer.Ordinal));
    }
}
