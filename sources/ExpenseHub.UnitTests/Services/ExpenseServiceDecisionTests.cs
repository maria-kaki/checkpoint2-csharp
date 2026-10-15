using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Domain;
using ExpenseHub.Api.Dtos;
using ExpenseHub.Api.Responses;
using ExpenseHub.Api.Security;
using ExpenseHub.Api.Services;
using ExpenseHub.UnitTests.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Services;

/// <summary>Aprovação e reprovação no serviço: role, ownership, estado e justificativa.</summary>
[TestClass]
public sealed class ExpenseServiceDecisionTests
{
    private const string ValidJustification = "Documento fiscal ilegível.";

    private readonly FakeExpenseRepository _repository = new();
    private readonly FixedTimeProvider _clock = new();
    private readonly ExpenseService _service;

    /// <summary>Cria o serviço com repositório em memória e relógio fixo.</summary>
    public ExpenseServiceDecisionTests()
    {
        _service = new ExpenseService(_repository, _clock);
    }

    /// <summary>Approver não proprietário aprova um Submitted.</summary>
    /// <returns>Tarefa assíncrona.</returns>
    [TestMethod]
    public async Task ApproveAsync_ApproverNaoProprietario_AprovaSubmitted()
    {
        Expense expense = SeedSubmitted("ana");

        ServiceResult<ExpenseResponse> result = await _service.ApproveAsync(
            TestUsers.With("bruno", AppRoles.Approver),
            expense.Id,
            CancellationToken.None);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual(ExpenseStatus.Approved, result.Value!.Status);
        Assert.AreEqual(1, _repository.SaveCount);

        ExpenseHistory history = expense.History.Single(item => item.Action == ExpenseAction.Approved);
        Assert.AreEqual("bruno", history.ActorId);
        Assert.AreEqual(_clock.UtcNow, history.OccurredAtUtc);
    }

    /// <summary>Approver não proprietário reprova um Submitted e persiste a justificativa.</summary>
    /// <returns>Tarefa assíncrona.</returns>
    [TestMethod]
    public async Task RejectAsync_ApproverNaoProprietario_ReprovaSubmitted()
    {
        Expense expense = SeedSubmitted("ana");

        ServiceResult<ExpenseResponse> result = await _service.RejectAsync(
            TestUsers.With("bruno", AppRoles.Approver),
            expense.Id,
            Request("  " + ValidJustification + "  "),
            CancellationToken.None);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual(ExpenseStatus.Rejected, result.Value!.Status);
        Assert.AreEqual(1, _repository.SaveCount);

        ExpenseHistory history = expense.History.Single(item => item.Action == ExpenseAction.Rejected);
        Assert.AreEqual(ValidJustification, history.Justification);
        Assert.AreEqual("bruno", history.ActorId);
        Assert.AreEqual(_clock.UtcNow, history.OccurredAtUtc);
    }

    /// <summary>Proprietário com role Approver não decide a própria despesa.</summary>
    /// <param name="approve">Verdadeiro para aprovar; falso para reprovar.</param>
    /// <returns>Tarefa assíncrona.</returns>
    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task DecisionAsync_ProprietarioComRolesAcumuladas_RetornaForbidden(bool approve)
    {
        Expense expense = SeedSubmitted("ana");
        CurrentUser owner = TestUsers.With("ana", AppRoles.Employee, AppRoles.Approver);

        ServiceResult<ExpenseResponse> result = approve
            ? await _service.ApproveAsync(owner, expense.Id, CancellationToken.None)
            : await _service.RejectAsync(owner, expense.Id, Request(), CancellationToken.None);

        Assert.AreEqual(ServiceErrorType.Forbidden, result.Error?.Type);
        Assert.AreEqual(ExpenseStatus.Submitted, expense.Status);
        Assert.AreEqual(0, _repository.SaveCount);
    }

    /// <summary>Perfis sem Approver não decidem reembolsos.</summary>
    /// <param name="role">Role sem permissão.</param>
    /// <returns>Tarefa assíncrona.</returns>
    [TestMethod]
    [DataRow(AppRoles.Employee)]
    [DataRow(AppRoles.Finance)]
    [DataRow(AppRoles.Auditor)]
    [DataRow(AppRoles.Admin)]
    public async Task ApproveAsync_SemRoleApprover_RetornaForbidden(string role)
    {
        Expense expense = SeedSubmitted("ana");

        ServiceResult<ExpenseResponse> result = await _service.ApproveAsync(
            TestUsers.With("bruno", role),
            expense.Id,
            CancellationToken.None);

        Assert.AreEqual(ServiceErrorType.Forbidden, result.Error?.Type);
        Assert.AreEqual(0, _repository.SaveCount);
    }

    /// <summary>Estado diferente de Submitted produz conflito e não cria histórico.</summary>
    /// <param name="status">Estado incompatível.</param>
    /// <returns>Tarefa assíncrona.</returns>
    [TestMethod]
    [DataRow(ExpenseStatus.Draft)]
    [DataRow(ExpenseStatus.Approved)]
    [DataRow(ExpenseStatus.Rejected)]
    [DataRow(ExpenseStatus.Paid)]
    public async Task ApproveAsync_EstadoIncompativel_RetornaConflict(ExpenseStatus status)
    {
        Expense expense = _repository.Seed(ExpenseFactory.AtStatus(
            "ana",
            _repository.Category(2),
            _clock,
            status));
        int historyCount = expense.History.Count;

        ServiceResult<ExpenseResponse> result = await _service.ApproveAsync(
            TestUsers.With("bruno", AppRoles.Approver),
            expense.Id,
            CancellationToken.None);

        Assert.AreEqual(ServiceErrorType.Conflict, result.Error?.Type);
        Assert.HasCount(historyCount, expense.History);
        Assert.AreEqual(0, _repository.SaveCount);
    }

    /// <summary>Repetir uma aprovação retorna conflito sem duplicar o histórico.</summary>
    /// <returns>Tarefa assíncrona.</returns>
    [TestMethod]
    public async Task ApproveAsync_Repetido_RetornaConflictSemDuplicarHistorico()
    {
        Expense expense = SeedSubmitted("ana");
        CurrentUser approver = TestUsers.With("bruno", AppRoles.Approver);
        await _service.ApproveAsync(approver, expense.Id, CancellationToken.None);

        ServiceResult<ExpenseResponse> result = await _service.ApproveAsync(
            approver,
            expense.Id,
            CancellationToken.None);

        Assert.AreEqual(ServiceErrorType.Conflict, result.Error?.Type);
        Assert.AreEqual(1, expense.History.Count(item => item.Action == ExpenseAction.Approved));
        Assert.AreEqual(1, _repository.SaveCount);
    }

    /// <summary>Repetir uma reprovação retorna conflito sem duplicar o histórico.</summary>
    /// <returns>Tarefa assíncrona.</returns>
    [TestMethod]
    public async Task RejectAsync_Repetido_RetornaConflictSemDuplicarHistorico()
    {
        Expense expense = SeedSubmitted("ana");
        CurrentUser approver = TestUsers.With("bruno", AppRoles.Approver);
        await _service.RejectAsync(approver, expense.Id, Request(), CancellationToken.None);

        ServiceResult<ExpenseResponse> result = await _service.RejectAsync(
            approver,
            expense.Id,
            Request("Justificativa válida diferente."),
            CancellationToken.None);

        Assert.AreEqual(ServiceErrorType.Conflict, result.Error?.Type);
        Assert.AreEqual(1, expense.History.Count(item => item.Action == ExpenseAction.Rejected));
        Assert.AreEqual(ValidJustification, expense.History.Single(item => item.Action == ExpenseAction.Rejected).Justification);
        Assert.AreEqual(1, _repository.SaveCount);
    }

    /// <summary>Justificativa ausente, curta ou longa retorna validação sem alterar dados.</summary>
    /// <param name="justificationKind">Tipo de entrada inválida.</param>
    /// <returns>Tarefa assíncrona.</returns>
    [TestMethod]
    [DataRow("null")]
    [DataRow("short")]
    [DataRow("long")]
    public async Task RejectAsync_JustificativaInvalida_RetornaValidation(string justificationKind)
    {
        Expense expense = SeedSubmitted("ana");
        string? justification = justificationKind switch
        {
            "null" => null,
            "short" => "curta",
            _ => new string('x', ExpenseHistory.JustificationMaxLength + 1),
        };

        ServiceResult<ExpenseResponse> result = await _service.RejectAsync(
            TestUsers.With("bruno", AppRoles.Approver),
            expense.Id,
            Request(justification),
            CancellationToken.None);

        Assert.AreEqual(ServiceErrorType.Validation, result.Error?.Type);
        Assert.IsTrue(result.Error!.Errors!.ContainsKey("justification"));
        Assert.AreEqual(ExpenseStatus.Submitted, expense.Status);
        Assert.AreEqual(0, _repository.SaveCount);
    }

    /// <summary>Reembolso inexistente retorna 404.</summary>
    /// <returns>Tarefa assíncrona.</returns>
    [TestMethod]
    public async Task ApproveAsync_Inexistente_RetornaNotFound()
    {
        ServiceResult<ExpenseResponse> result = await _service.ApproveAsync(
            TestUsers.With("bruno", AppRoles.Approver),
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.AreEqual(ServiceErrorType.NotFound, result.Error?.Type);
    }

    /// <summary>Conflito de concorrência é devolvido como 409.</summary>
    /// <returns>Tarefa assíncrona.</returns>
    [TestMethod]
    public async Task RejectAsync_ConflitoDeConcorrencia_RetornaConflict()
    {
        Expense expense = SeedSubmitted("ana");
        _repository.SimulateConcurrencyConflict = true;

        ServiceResult<ExpenseResponse> result = await _service.RejectAsync(
            TestUsers.With("bruno", AppRoles.Approver),
            expense.Id,
            Request(),
            CancellationToken.None);

        Assert.AreEqual(ServiceErrorType.Conflict, result.Error?.Type);
    }

    private Expense SeedSubmitted(string ownerId)
    {
        Expense expense = _repository.Seed(ExpenseFactory.Draft(ownerId, _repository.Category(2), _clock));
        expense.Submit(ownerId, _clock.UtcNow);
        return expense;
    }

    private static RejectExpenseRequest Request(string? justification = ValidJustification)
    {
        return new RejectExpenseRequest { Justification = justification };
    }
}
