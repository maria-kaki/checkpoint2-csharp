using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Domain;
using ExpenseHub.Api.Responses;
using ExpenseHub.Api.Security;
using ExpenseHub.Api.Services;
using ExpenseHub.UnitTests.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Services;

/// <summary>
/// Listagem e detalhe de reembolsos conforme o perfil, sem vazar recursos fora do escopo.
/// </summary>
[TestClass]
public sealed class ExpenseServiceQueryTests
{
    private readonly FakeExpenseRepository _repository = new();
    private readonly FixedTimeProvider _clock = new();
    private readonly ExpenseService _service;
    private readonly Expense _anaDraft;
    private readonly Expense _anaSubmitted;
    private readonly Expense _brunoDraft;

    /// <summary>Prepara um Draft e um Submitted da Ana e um Draft do Bruno.</summary>
    public ExpenseServiceQueryTests()
    {
        _service = new ExpenseService(_repository, _clock);
        _anaDraft = _repository.Seed(ExpenseFactory.Draft("ana", _repository.Category(1), _clock));
        _anaSubmitted = _repository.Seed(ExpenseFactory.Draft("ana", _repository.Category(2), _clock));
        _anaSubmitted.Submit("ana", _clock.UtcNow);
        _brunoDraft = _repository.Seed(ExpenseFactory.Draft("bruno", _repository.Category(3), _clock));
    }

    /// <summary>Employee lista apenas os próprios reembolsos, em qualquer estado.</summary>
    /// <returns>Tarefa assíncrona.</returns>
    [TestMethod]
    public async Task ListAsync_Employee_ListaSomenteOsProprios()
    {
        IReadOnlyList<ExpenseResponse> expenses = await ListAsync(TestUsers.Employee("ana"));

        Assert.AreEqual(Ids(_anaDraft, _anaSubmitted), Ids(expenses));
    }

    /// <summary>Approver lista somente reembolsos Submitted.</summary>
    /// <returns>Tarefa assíncrona.</returns>
    [TestMethod]
    public async Task ListAsync_Approver_ListaSomenteSubmitted()
    {
        IReadOnlyList<ExpenseResponse> expenses = await ListAsync(TestUsers.With("apro", AppRoles.Approver));

        Assert.AreEqual(Ids(_anaSubmitted), Ids(expenses));
    }

    /// <summary>Roles acumuladas recebem a união dos escopos: os próprios e os Submitted.</summary>
    /// <returns>Tarefa assíncrona.</returns>
    [TestMethod]
    public async Task ListAsync_EmployeeEApprover_ListaUniaoDosEscopos()
    {
        IReadOnlyList<ExpenseResponse> expenses = await ListAsync(
            TestUsers.With("bruno", AppRoles.Employee, AppRoles.Approver));

        Assert.AreEqual(Ids(_anaSubmitted, _brunoDraft), Ids(expenses));
    }

    /// <summary>Finance não enxerga reembolsos em Draft ou Submitted.</summary>
    /// <returns>Tarefa assíncrona.</returns>
    [TestMethod]
    public async Task ListAsync_Finance_NaoListaDraftNemSubmitted()
    {
        IReadOnlyList<ExpenseResponse> expenses = await ListAsync(TestUsers.With("fin", AppRoles.Finance));

        Assert.IsEmpty(expenses);
    }

    /// <summary>Auditor lista todos os reembolsos.</summary>
    /// <returns>Tarefa assíncrona.</returns>
    [TestMethod]
    public async Task ListAsync_Auditor_ListaTodos()
    {
        IReadOnlyList<ExpenseResponse> expenses = await ListAsync(TestUsers.With("aud", AppRoles.Auditor));

        Assert.AreEqual(Ids(_anaDraft, _anaSubmitted, _brunoDraft), Ids(expenses));
    }

    /// <summary>Admin sem outra role e usuário sem role não consultam reembolsos (403).</summary>
    /// <param name="role">Role do usuário, ou vazio para nenhuma.</param>
    /// <returns>Tarefa assíncrona.</returns>
    [TestMethod]
    [DataRow(AppRoles.Admin)]
    [DataRow("")]
    public async Task ListAsync_SemPerfilDeLeitura_RetornaForbidden(string role)
    {
        CurrentUser user = string.IsNullOrEmpty(role) ? TestUsers.With("x") : TestUsers.With("x", role);

        ServiceResult<IReadOnlyList<ExpenseResponse>> result = await _service.ListAsync(user, CancellationToken.None);

        Assert.AreEqual(ServiceErrorType.Forbidden, result.Error?.Type);
    }

    /// <summary>O proprietário consulta o detalhe do próprio reembolso.</summary>
    /// <returns>Tarefa assíncrona.</returns>
    [TestMethod]
    public async Task GetAsync_Proprietario_RetornaDetalhe()
    {
        ServiceResult<ExpenseResponse> result = await _service.GetAsync(
            TestUsers.Employee("ana"),
            _anaDraft.Id,
            CancellationToken.None);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual(_anaDraft.Id, result.Value!.Id);
    }

    /// <summary>
    /// Trocar o identificador na URL não expõe dados: outro Employee, Approver diante de um Draft e
    /// Finance diante de um Submitted recebem o mesmo 404 de um recurso inexistente.
    /// </summary>
    /// <param name="userId">Usuário.</param>
    /// <param name="role">Role do usuário.</param>
    /// <param name="submitted">Consulta o reembolso enviado em vez do rascunho.</param>
    /// <returns>Tarefa assíncrona.</returns>
    [TestMethod]
    [DataRow("bruno", AppRoles.Employee, false)]
    [DataRow("bruno", AppRoles.Employee, true)]
    [DataRow("apro", AppRoles.Approver, false)]
    [DataRow("fin", AppRoles.Finance, true)]
    public async Task GetAsync_ForaDoEscopo_RetornaNotFound(string userId, string role, bool submitted)
    {
        Expense target = submitted ? _anaSubmitted : _anaDraft;

        ServiceResult<ExpenseResponse> result = await _service.GetAsync(
            TestUsers.With(userId, role),
            target.Id,
            CancellationToken.None);

        Assert.AreEqual(ServiceErrorType.NotFound, result.Error?.Type);
        Assert.IsNull(result.Value);
    }

    /// <summary>Approver consulta o detalhe de um reembolso Submitted.</summary>
    /// <returns>Tarefa assíncrona.</returns>
    [TestMethod]
    public async Task GetAsync_ApproverComSubmitted_RetornaDetalhe()
    {
        ServiceResult<ExpenseResponse> result = await _service.GetAsync(
            TestUsers.With("apro", AppRoles.Approver),
            _anaSubmitted.Id,
            CancellationToken.None);

        Assert.IsTrue(result.Succeeded);
    }

    /// <summary>Recurso inexistente retorna 404.</summary>
    /// <returns>Tarefa assíncrona.</returns>
    [TestMethod]
    public async Task GetAsync_Inexistente_RetornaNotFound()
    {
        ServiceResult<ExpenseResponse> result = await _service.GetAsync(
            TestUsers.With("aud", AppRoles.Auditor),
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.AreEqual(ServiceErrorType.NotFound, result.Error?.Type);
    }

    /// <summary>Admin sem outra role não consulta o detalhe (403).</summary>
    /// <returns>Tarefa assíncrona.</returns>
    [TestMethod]
    public async Task GetAsync_SomenteAdmin_RetornaForbidden()
    {
        ServiceResult<ExpenseResponse> result = await _service.GetAsync(
            TestUsers.With("admin", AppRoles.Admin),
            _anaDraft.Id,
            CancellationToken.None);

        Assert.AreEqual(ServiceErrorType.Forbidden, result.Error?.Type);
    }

    private async Task<IReadOnlyList<ExpenseResponse>> ListAsync(CurrentUser user)
    {
        ServiceResult<IReadOnlyList<ExpenseResponse>> result = await _service.ListAsync(user, CancellationToken.None);
        Assert.IsTrue(result.Succeeded);
        return result.Value!;
    }

    private static string Ids(params Expense[] expenses)
    {
        return string.Join(",", expenses.Select(expense => expense.Id).Order());
    }

    private static string Ids(IEnumerable<ExpenseResponse> expenses)
    {
        return string.Join(",", expenses.Select(expense => expense.Id).Order());
    }
}
