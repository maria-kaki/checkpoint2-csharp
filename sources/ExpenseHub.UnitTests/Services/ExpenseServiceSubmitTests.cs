using System;
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
/// Envio de rascunhos (Draft → Submitted) no serviço: role, visibilidade, ownership e estado.
/// </summary>
[TestClass]
public sealed class ExpenseServiceSubmitTests
{
    private readonly FakeExpenseRepository _repository = new();
    private readonly FixedTimeProvider _clock = new();
    private readonly ExpenseService _service;

    /// <summary>Cria o serviço com repositório em memória e relógio fixo.</summary>
    public ExpenseServiceSubmitTests()
    {
        _service = new ExpenseService(_repository, _clock);
    }

    /// <summary>O proprietário envia o próprio Draft, que passa a Submitted.</summary>
    /// <returns>Tarefa assíncrona.</returns>
    [TestMethod]
    public async Task SubmitAsync_Proprietario_EnviaDraft()
    {
        Expense expense = SeedDraft("ana");

        ServiceResult<ExpenseResponse> result = await _service.SubmitAsync(
            TestUsers.Employee("ana"),
            expense.Id,
            CancellationToken.None);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual(ExpenseStatus.Submitted, result.Value!.Status);
        Assert.AreEqual(ExpenseStatus.Submitted, expense.Status);
        Assert.AreEqual(1, _repository.SaveCount);
    }

    /// <summary>Envio repetido retorna 409 sem persistir nem duplicar o histórico.</summary>
    /// <returns>Tarefa assíncrona.</returns>
    [TestMethod]
    public async Task SubmitAsync_Repetido_RetornaConflictSemDuplicarHistorico()
    {
        Expense expense = SeedDraft("ana");
        CurrentUser ana = TestUsers.Employee("ana");
        await _service.SubmitAsync(ana, expense.Id, CancellationToken.None);

        ServiceResult<ExpenseResponse> result = await _service.SubmitAsync(ana, expense.Id, CancellationToken.None);

        Assert.AreEqual(ServiceErrorType.Conflict, result.Error?.Type);
        Assert.AreEqual(1, expense.History.Count(history => history.Action == ExpenseAction.Submitted));
        Assert.AreEqual(1, _repository.SaveCount);
    }

    /// <summary>Outro Employee não enxerga o Draft e recebe 404.</summary>
    /// <returns>Tarefa assíncrona.</returns>
    [TestMethod]
    public async Task SubmitAsync_OutroEmployee_RetornaNotFound()
    {
        Expense expense = SeedDraft("ana");

        ServiceResult<ExpenseResponse> result = await _service.SubmitAsync(
            TestUsers.Employee("bruno"),
            expense.Id,
            CancellationToken.None);

        Assert.AreEqual(ServiceErrorType.NotFound, result.Error?.Type);
        Assert.AreEqual(ExpenseStatus.Draft, expense.Status);
    }

    /// <summary>
    /// Employee que também é Approver enxerga o reembolso enviado de outra pessoa, mas não pode
    /// executar ações do proprietário: 403, antes mesmo da checagem de estado.
    /// </summary>
    /// <returns>Tarefa assíncrona.</returns>
    [TestMethod]
    public async Task SubmitAsync_NaoProprietarioQueEnxergaOReembolso_RetornaForbidden()
    {
        Expense expense = SeedDraft("ana");
        expense.Submit("ana", _clock.UtcNow);

        ServiceResult<ExpenseResponse> result = await _service.SubmitAsync(
            TestUsers.With("duda", AppRoles.Employee, AppRoles.Approver),
            expense.Id,
            CancellationToken.None);

        Assert.AreEqual(ServiceErrorType.Forbidden, result.Error?.Type);
    }

    /// <summary>Somente Employee envia: Auditor não ganha escrita e Admin não ganha acesso funcional.</summary>
    /// <param name="role">Role sem permissão de envio.</param>
    /// <returns>Tarefa assíncrona.</returns>
    [TestMethod]
    [DataRow(AppRoles.Auditor)]
    [DataRow(AppRoles.Admin)]
    [DataRow(AppRoles.Approver)]
    [DataRow(AppRoles.Finance)]
    public async Task SubmitAsync_SemRoleEmployee_RetornaForbidden(string role)
    {
        Expense expense = SeedDraft("ana");

        ServiceResult<ExpenseResponse> result = await _service.SubmitAsync(
            TestUsers.With("outro", role),
            expense.Id,
            CancellationToken.None);

        Assert.AreEqual(ServiceErrorType.Forbidden, result.Error?.Type);
        Assert.AreEqual(ExpenseStatus.Draft, expense.Status);
    }

    /// <summary>Reembolso inexistente retorna 404.</summary>
    /// <returns>Tarefa assíncrona.</returns>
    [TestMethod]
    public async Task SubmitAsync_Inexistente_RetornaNotFound()
    {
        ServiceResult<ExpenseResponse> result = await _service.SubmitAsync(
            TestUsers.Employee("ana"),
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.AreEqual(ServiceErrorType.NotFound, result.Error?.Type);
    }

    /// <summary>Se outra operação alterou o reembolso ao mesmo tempo, o envio retorna conflito.</summary>
    /// <returns>Tarefa assíncrona.</returns>
    [TestMethod]
    public async Task SubmitAsync_ConflitoDeConcorrencia_RetornaConflict()
    {
        Expense expense = SeedDraft("ana");
        _repository.SimulateConcurrencyConflict = true;

        ServiceResult<ExpenseResponse> result = await _service.SubmitAsync(
            TestUsers.Employee("ana"),
            expense.Id,
            CancellationToken.None);

        Assert.AreEqual(ServiceErrorType.Conflict, result.Error?.Type);
    }

    /// <summary>Reembolso já enviado não é editado: 409.</summary>
    /// <returns>Tarefa assíncrona.</returns>
    [TestMethod]
    public async Task UpdateAsync_ReembolsoEnviado_RetornaConflict()
    {
        Expense expense = SeedDraft("ana");
        expense.Submit("ana", _clock.UtcNow);

        ServiceResult<ExpenseResponse> result = await _service.UpdateAsync(
            TestUsers.Employee("ana"),
            expense.Id,
            ExpenseFactory.Request(amount: 1m),
            CancellationToken.None);

        Assert.AreEqual(ServiceErrorType.Conflict, result.Error?.Type);
        Assert.AreEqual(87.50m, expense.Amount);
    }

    private Expense SeedDraft(string ownerId)
    {
        return _repository.Seed(ExpenseFactory.Draft(ownerId, _repository.Category(2), _clock));
    }
}
