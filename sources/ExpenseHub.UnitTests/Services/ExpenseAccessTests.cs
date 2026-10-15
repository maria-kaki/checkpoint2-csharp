using ExpenseHub.Api.Domain;
using ExpenseHub.Api.Security;
using ExpenseHub.Api.Services;
using ExpenseHub.UnitTests.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Services;

/// <summary>Autorização contextual de decisão e pagamento.</summary>
[TestClass]
public sealed class ExpenseAccessTests
{
    private readonly FakeExpenseRepository _repository = new();
    private readonly FixedTimeProvider _clock = new();

    /// <summary>Somente os perfis funcionais recebem acesso de leitura.</summary>
    /// <param name="role">Role avaliada.</param>
    /// <param name="expected">Resultado esperado.</param>
    [TestMethod]
    [DataRow(AppRoles.Employee, true)]
    [DataRow(AppRoles.Approver, true)]
    [DataRow(AppRoles.Finance, true)]
    [DataRow(AppRoles.Auditor, true)]
    [DataRow(AppRoles.Admin, false)]
    [DataRow("", false)]
    public void CanRead_AplicaMatrizDePerfis(string role, bool expected)
    {
        CurrentUser user = string.IsNullOrEmpty(role) ? TestUsers.With("usuario") : TestUsers.With("usuario", role);

        Assert.AreEqual(expected, ExpenseAccess.CanRead(user));
    }

    /// <summary>Approver não proprietário decide um reembolso Submitted.</summary>
    [TestMethod]
    public void ValidateDecision_ApproverNaoProprietarioEmSubmitted_Permite()
    {
        Expense expense = ExpenseAt(ExpenseStatus.Submitted);

        ServiceError? error = ExpenseAccess.ValidateDecision(
            TestUsers.With("bruno", AppRoles.Approver),
            expense);

        Assert.IsNull(error);
    }

    /// <summary>Acumular Employee e Approver não permite decidir o próprio reembolso.</summary>
    [TestMethod]
    public void ValidateDecision_ProprietarioComRolesAcumuladas_RetornaForbidden()
    {
        Expense expense = ExpenseAt(ExpenseStatus.Submitted);

        ServiceError? error = ExpenseAccess.ValidateDecision(
            TestUsers.With("ana", AppRoles.Employee, AppRoles.Approver),
            expense);

        Assert.AreEqual(ServiceErrorType.Forbidden, error?.Type);
    }

    /// <summary>Somente a role Approver concede a operação de decisão.</summary>
    /// <param name="role">Role sem permissão para decidir.</param>
    [TestMethod]
    [DataRow(AppRoles.Employee)]
    [DataRow(AppRoles.Finance)]
    [DataRow(AppRoles.Auditor)]
    [DataRow(AppRoles.Admin)]
    public void ValidateDecision_SemRoleApprover_RetornaForbidden(string role)
    {
        Expense expense = ExpenseAt(ExpenseStatus.Submitted);

        ServiceError? error = ExpenseAccess.ValidateDecision(TestUsers.With("bruno", role), expense);

        Assert.AreEqual(ServiceErrorType.Forbidden, error?.Type);
    }

    /// <summary>Approver não decide um reembolso fora de Submitted.</summary>
    /// <param name="status">Estado incompatível.</param>
    [TestMethod]
    [DataRow(ExpenseStatus.Draft)]
    [DataRow(ExpenseStatus.Approved)]
    [DataRow(ExpenseStatus.Rejected)]
    [DataRow(ExpenseStatus.Paid)]
    public void ValidateDecision_EstadoIncompativel_RetornaConflict(ExpenseStatus status)
    {
        ServiceError? error = ExpenseAccess.ValidateDecision(
            TestUsers.With("bruno", AppRoles.Approver),
            ExpenseAt(status));

        Assert.AreEqual(ServiceErrorType.Conflict, error?.Type);
    }

    /// <summary>Finance não proprietário paga um reembolso Approved.</summary>
    [TestMethod]
    public void ValidatePayment_FinanceNaoProprietarioEmApproved_Permite()
    {
        ServiceError? error = ExpenseAccess.ValidatePayment(
            TestUsers.With("bruno", AppRoles.Finance),
            ExpenseAt(ExpenseStatus.Approved));

        Assert.IsNull(error);
    }

    /// <summary>Acumular Employee e Finance não permite pagar o próprio reembolso.</summary>
    [TestMethod]
    public void ValidatePayment_ProprietarioComRolesAcumuladas_RetornaForbidden()
    {
        ServiceError? error = ExpenseAccess.ValidatePayment(
            TestUsers.With("ana", AppRoles.Employee, AppRoles.Finance),
            ExpenseAt(ExpenseStatus.Approved));

        Assert.AreEqual(ServiceErrorType.Forbidden, error?.Type);
    }

    /// <summary>Somente a role Finance concede a operação de pagamento.</summary>
    /// <param name="role">Role sem permissão para pagar.</param>
    [TestMethod]
    [DataRow(AppRoles.Employee)]
    [DataRow(AppRoles.Approver)]
    [DataRow(AppRoles.Auditor)]
    [DataRow(AppRoles.Admin)]
    public void ValidatePayment_SemRoleFinance_RetornaForbidden(string role)
    {
        ServiceError? error = ExpenseAccess.ValidatePayment(
            TestUsers.With("bruno", role),
            ExpenseAt(ExpenseStatus.Approved));

        Assert.AreEqual(ServiceErrorType.Forbidden, error?.Type);
    }

    /// <summary>Finance não paga um reembolso fora de Approved.</summary>
    /// <param name="status">Estado incompatível.</param>
    [TestMethod]
    [DataRow(ExpenseStatus.Draft)]
    [DataRow(ExpenseStatus.Submitted)]
    [DataRow(ExpenseStatus.Rejected)]
    [DataRow(ExpenseStatus.Paid)]
    public void ValidatePayment_EstadoIncompativel_RetornaConflict(ExpenseStatus status)
    {
        ServiceError? error = ExpenseAccess.ValidatePayment(
            TestUsers.With("bruno", AppRoles.Finance),
            ExpenseAt(status));

        Assert.AreEqual(ServiceErrorType.Conflict, error?.Type);
    }

    private Expense ExpenseAt(ExpenseStatus status)
    {
        return ExpenseFactory.AtStatus("ana", _repository.Category(2), _clock, status);
    }
}
