using System;
using System.Linq;
using ExpenseHub.Api.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Security;

/// <summary>
/// Catálogo de roles conhecidas.
/// </summary>
[TestClass]
public sealed class AppRolesTests
{
    /// <summary>As cinco roles do contrato precisam existir.</summary>
    [TestMethod]
    public void All_ContemAsCincoRolesDoContrato()
    {
        Assert.AreEqual(
            "Admin,Approver,Auditor,Employee,Finance",
            string.Join(",", AppRoles.All.Order(StringComparer.Ordinal)));
    }

    /// <summary>Uma role fora do catálogo não é reconhecida.</summary>
    /// <param name="role">Valor informado.</param>
    [TestMethod]
    [DataRow("Manager")]
    [DataRow("")]
    [DataRow(null)]
    public void FindKnown_ComRoleForaDoCatalogo_RetornaNulo(string? role)
    {
        Assert.IsNull(AppRoles.FindKnown(role));
    }

    /// <summary>Espaços e diferença de caixa não impedem o reconhecimento.</summary>
    [TestMethod]
    public void FindKnown_ComEspacosECaixaDiferente_RetornaNomeCanonico()
    {
        Assert.AreEqual(AppRoles.Approver, AppRoles.FindKnown("  aPPROVER "));
    }
}
