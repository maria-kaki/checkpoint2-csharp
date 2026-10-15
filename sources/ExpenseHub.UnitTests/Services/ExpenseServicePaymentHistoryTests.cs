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

/// <summary>Pagamento e consulta autorizada do histórico.</summary>
[TestClass]
public sealed class ExpenseServicePaymentHistoryTests
{
    private readonly FakeExpenseRepository _repository = new();
    private readonly FixedTimeProvider _clock = new();
    private readonly ExpenseService _service;

    /// <summary>Cria o serviço com repositório em memória e relógio fixo.</summary>
    public ExpenseServicePaymentHistoryTests()
    {
        _service = new ExpenseService(_repository, _clock);
    }

    /// <summary>Finance não proprietário paga um Approved com ator e horário do servidor.</summary>
    /// <returns>Tarefa assíncrona.</returns>
    [TestMethod]
    public async Task PayAsync_FinanceNaoProprietario_PagaApproved()
    {
        Expense expense = SeedApproved("ana");

        ServiceResult<ExpenseResponse> result = await _service.PayAsync(
            TestUsers.With("carla", AppRoles.Finance),
            expense.Id,
            CancellationToken.None);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual(ExpenseStatus.Paid, result.Value!.Status);
        Assert.AreEqual(1, _repository.SaveCount);
        Assert.IsNotNull(expense.Payment);
        Assert.AreEqual("carla", expense.Payment.PaidById);
        Assert.AreEqual(_clock.UtcNow, expense.Payment.PaidAtUtc);
        Assert.AreEqual(expense.Amount, expense.Payment.Amount);
    }

    /// <summary>Proprietário com role Finance não paga a própria despesa.</summary>
    /// <returns>Tarefa assíncrona.</returns>
    [TestMethod]
    public async Task PayAsync_ProprietarioComRolesAcumuladas_RetornaForbidden()
    {
        Expense expense = SeedApproved("ana");

        ServiceResult<ExpenseResponse> result = await _service.PayAsync(
            TestUsers.With("ana", AppRoles.Employee, AppRoles.Finance),
            expense.Id,
            CancellationToken.None);

        Assert.AreEqual(ServiceErrorType.Forbidden, result.Error?.Type);
        Assert.AreEqual(ExpenseStatus.Approved, expense.Status);
        Assert.IsNull(expense.Payment);
        Assert.AreEqual(0, _repository.SaveCount);
    }

    /// <summary>Perfis sem Finance não registram pagamentos.</summary>
    /// <param name="role">Role sem permissão.</param>
    /// <returns>Tarefa assíncrona.</returns>
    [TestMethod]
    [DataRow(AppRoles.Employee)]
    [DataRow(AppRoles.Approver)]
    [DataRow(AppRoles.Auditor)]
    [DataRow(AppRoles.Admin)]
    public async Task PayAsync_SemRoleFinance_RetornaForbidden(string role)
    {
        Expense expense = SeedApproved("ana");

        ServiceResult<ExpenseResponse> result = await _service.PayAsync(
            TestUsers.With("carla", role),
            expense.Id,
            CancellationToken.None);

        Assert.AreEqual(ServiceErrorType.Forbidden, result.Error?.Type);
        Assert.AreEqual(0, _repository.SaveCount);
    }

    /// <summary>Estado diferente de Approved produz conflito sem pagamento nem histórico.</summary>
    /// <param name="status">Estado incompatível.</param>
    /// <returns>Tarefa assíncrona.</returns>
    [TestMethod]
    [DataRow(ExpenseStatus.Draft)]
    [DataRow(ExpenseStatus.Submitted)]
    [DataRow(ExpenseStatus.Rejected)]
    [DataRow(ExpenseStatus.Paid)]
    public async Task PayAsync_EstadoIncompativel_RetornaConflict(ExpenseStatus status)
    {
        Expense expense = _repository.Seed(ExpenseFactory.AtStatus(
            "ana",
            _repository.Category(2),
            _clock,
            status));
        int historyCount = expense.History.Count;

        ServiceResult<ExpenseResponse> result = await _service.PayAsync(
            TestUsers.With("carla", AppRoles.Finance),
            expense.Id,
            CancellationToken.None);

        Assert.AreEqual(ServiceErrorType.Conflict, result.Error?.Type);
        Assert.IsNull(expense.Payment);
        Assert.HasCount(historyCount, expense.History);
        Assert.AreEqual(0, _repository.SaveCount);
    }

    /// <summary>Pagamento repetido retorna conflito sem duplicar pagamento ou histórico.</summary>
    /// <returns>Tarefa assíncrona.</returns>
    [TestMethod]
    public async Task PayAsync_Repetido_RetornaConflictSemDuplicarRegistros()
    {
        Expense expense = SeedApproved("ana");
        CurrentUser finance = TestUsers.With("carla", AppRoles.Finance);
        await _service.PayAsync(finance, expense.Id, CancellationToken.None);
        PaymentRecord payment = expense.Payment!;

        ServiceResult<ExpenseResponse> result = await _service.PayAsync(
            finance,
            expense.Id,
            CancellationToken.None);

        Assert.AreEqual(ServiceErrorType.Conflict, result.Error?.Type);
        Assert.AreSame(payment, expense.Payment);
        Assert.AreEqual(1, expense.History.Count(item => item.Action == ExpenseAction.Paid));
        Assert.AreEqual(1, _repository.SaveCount);
    }

    /// <summary>Reembolso inexistente retorna 404.</summary>
    /// <returns>Tarefa assíncrona.</returns>
    [TestMethod]
    public async Task PayAsync_Inexistente_RetornaNotFound()
    {
        ServiceResult<ExpenseResponse> result = await _service.PayAsync(
            TestUsers.With("carla", AppRoles.Finance),
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.AreEqual(ServiceErrorType.NotFound, result.Error?.Type);
    }

    /// <summary>Conflito de concorrência é devolvido como 409.</summary>
    /// <returns>Tarefa assíncrona.</returns>
    [TestMethod]
    public async Task PayAsync_ConflitoDeConcorrencia_RetornaConflict()
    {
        Expense expense = SeedApproved("ana");
        _repository.SimulateConcurrencyConflict = true;

        ServiceResult<ExpenseResponse> result = await _service.PayAsync(
            TestUsers.With("carla", AppRoles.Finance),
            expense.Id,
            CancellationToken.None);

        Assert.AreEqual(ServiceErrorType.Conflict, result.Error?.Type);
    }

    /// <summary>O proprietário consulta o histórico completo em ordem cronológica.</summary>
    /// <returns>Tarefa assíncrona.</returns>
    [TestMethod]
    public async Task GetHistoryAsync_Proprietario_RetornaHistoricoCronologico()
    {
        Expense expense = SeedPaidWithDistinctTimes("ana");

        ServiceResult<IReadOnlyList<ExpenseHistoryResponse>> result = await _service.GetHistoryAsync(
            TestUsers.Employee("ana"),
            expense.Id,
            CancellationToken.None);

        Assert.IsTrue(result.Succeeded);
        IReadOnlyList<ExpenseHistoryResponse> history = result.Value!;
        CollectionAssert.AreEqual(
            new[] { ExpenseAction.Created, ExpenseAction.Submitted, ExpenseAction.Approved, ExpenseAction.Paid },
            history.Select(item => item.Action).ToArray());
        Assert.IsTrue(history.Select(item => item.OccurredAtUtc).SequenceEqual(
            history.Select(item => item.OccurredAtUtc).Order()));
    }

    /// <summary>O contrato do histórico preserva ator, horários, estados, alterações e justificativa.</summary>
    /// <returns>Tarefa assíncrona.</returns>
    [TestMethod]
    public async Task GetHistoryAsync_ComEdicaoERejeicao_RetornaTodosOsDadosAuditaveis()
    {
        Expense expense = _repository.Seed(ExpenseFactory.Draft("ana", _repository.Category(2), _clock));
        DateTime updatedAt = _clock.UtcNow.AddMinutes(1);
        DateTime submittedAt = _clock.UtcNow.AddMinutes(2);
        DateTime rejectedAt = _clock.UtcNow.AddMinutes(3);
        expense.Update(
            "ana",
            _repository.Category(3),
            ExpenseFactory.ValidDescription,
            120m,
            _clock.Today,
            updatedAt);
        expense.Submit("ana", submittedAt);
        expense.Reject("bruno", "Documento fiscal ilegível.", rejectedAt);

        ServiceResult<IReadOnlyList<ExpenseHistoryResponse>> result = await _service.GetHistoryAsync(
            TestUsers.With("aud", AppRoles.Auditor),
            expense.Id,
            CancellationToken.None);

        Assert.IsTrue(result.Succeeded);
        IReadOnlyList<ExpenseHistoryResponse> history = result.Value!;
        Assert.HasCount(4, history);

        ExpenseHistoryResponse created = history[0];
        Assert.AreEqual(ExpenseAction.Created, created.Action);
        Assert.AreEqual("ana", created.ActorId);
        Assert.AreEqual(_clock.UtcNow, created.OccurredAtUtc);
        Assert.IsNull(created.PreviousStatus);
        Assert.AreEqual(ExpenseStatus.Draft, created.NewStatus);

        ExpenseHistoryResponse updated = history[1];
        Assert.AreEqual(ExpenseAction.Updated, updated.Action);
        Assert.AreEqual("ana", updated.ActorId);
        Assert.AreEqual(updatedAt, updated.OccurredAtUtc);
        Assert.AreEqual(ExpenseStatus.Draft, updated.PreviousStatus);
        Assert.AreEqual(ExpenseStatus.Draft, updated.NewStatus);
        Assert.Contains("categoria: \"Transporte\" → \"Hospedagem\"", updated.Changes!);
        Assert.Contains("valor: \"87.50\" → \"120.00\"", updated.Changes!);

        ExpenseHistoryResponse submitted = history[2];
        Assert.AreEqual(ExpenseAction.Submitted, submitted.Action);
        Assert.AreEqual("ana", submitted.ActorId);
        Assert.AreEqual(submittedAt, submitted.OccurredAtUtc);
        Assert.AreEqual(ExpenseStatus.Draft, submitted.PreviousStatus);
        Assert.AreEqual(ExpenseStatus.Submitted, submitted.NewStatus);

        ExpenseHistoryResponse rejected = history[3];
        Assert.AreEqual(ExpenseAction.Rejected, rejected.Action);
        Assert.AreEqual("bruno", rejected.ActorId);
        Assert.AreEqual(rejectedAt, rejected.OccurredAtUtc);
        Assert.AreEqual(ExpenseStatus.Submitted, rejected.PreviousStatus);
        Assert.AreEqual(ExpenseStatus.Rejected, rejected.NewStatus);
        Assert.AreEqual("Documento fiscal ilegível.", rejected.Justification);
    }

    /// <summary>Auditor consulta todo o histórico sem depender do estado.</summary>
    /// <returns>Tarefa assíncrona.</returns>
    [TestMethod]
    public async Task GetHistoryAsync_Auditor_RetornaHistorico()
    {
        Expense expense = SeedApproved("ana");

        ServiceResult<IReadOnlyList<ExpenseHistoryResponse>> result = await _service.GetHistoryAsync(
            TestUsers.With("aud", AppRoles.Auditor),
            expense.Id,
            CancellationToken.None);

        Assert.IsTrue(result.Succeeded);
        Assert.HasCount(3, result.Value!);
    }

    /// <summary>Auditor recebe 404 ao consultar o histórico de um reembolso inexistente.</summary>
    /// <returns>Tarefa assíncrona.</returns>
    [TestMethod]
    public async Task GetHistoryAsync_AuditorEReembolsoInexistente_RetornaNotFound()
    {
        ServiceResult<IReadOnlyList<ExpenseHistoryResponse>> result = await _service.GetHistoryAsync(
            TestUsers.With("aud", AppRoles.Auditor),
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.AreEqual(ServiceErrorType.NotFound, result.Error?.Type);
    }

    /// <summary>Approver consulta histórico de Submitted e Finance de Approved/Paid.</summary>
    /// <param name="status">Estado visível.</param>
    /// <param name="role">Role com visibilidade.</param>
    /// <returns>Tarefa assíncrona.</returns>
    [TestMethod]
    [DataRow(ExpenseStatus.Submitted, AppRoles.Approver)]
    [DataRow(ExpenseStatus.Approved, AppRoles.Finance)]
    [DataRow(ExpenseStatus.Paid, AppRoles.Finance)]
    public async Task GetHistoryAsync_PerfilComVisibilidade_RetornaHistorico(ExpenseStatus status, string role)
    {
        Expense expense = _repository.Seed(ExpenseFactory.AtStatus(
            "ana",
            _repository.Category(2),
            _clock,
            status));

        ServiceResult<IReadOnlyList<ExpenseHistoryResponse>> result = await _service.GetHistoryAsync(
            TestUsers.With("outro", role),
            expense.Id,
            CancellationToken.None);

        Assert.IsTrue(result.Succeeded);
    }

    /// <summary>Reembolso fora do escopo não tem histórico exposto.</summary>
    /// <param name="userId">Identificador do usuário.</param>
    /// <param name="role">Role sem visibilidade para o alvo.</param>
    /// <returns>Tarefa assíncrona.</returns>
    [TestMethod]
    [DataRow("bruno", AppRoles.Employee)]
    [DataRow("apro", AppRoles.Approver)]
    [DataRow("fin", AppRoles.Finance)]
    public async Task GetHistoryAsync_ForaDoEscopo_RetornaNotFound(string userId, string role)
    {
        Expense expense = _repository.Seed(ExpenseFactory.Draft("ana", _repository.Category(2), _clock));

        ServiceResult<IReadOnlyList<ExpenseHistoryResponse>> result = await _service.GetHistoryAsync(
            TestUsers.With(userId, role),
            expense.Id,
            CancellationToken.None);

        Assert.AreEqual(ServiceErrorType.NotFound, result.Error?.Type);
    }

    /// <summary>Admin sem outra role e usuário sem role recebem 403.</summary>
    /// <param name="role">Role avaliada ou vazio para nenhuma.</param>
    /// <returns>Tarefa assíncrona.</returns>
    [TestMethod]
    [DataRow(AppRoles.Admin)]
    [DataRow("")]
    public async Task GetHistoryAsync_SemPerfilDeLeitura_RetornaForbidden(string role)
    {
        Expense expense = _repository.Seed(ExpenseFactory.Draft("ana", _repository.Category(2), _clock));
        CurrentUser user = string.IsNullOrEmpty(role) ? TestUsers.With("x") : TestUsers.With("x", role);

        ServiceResult<IReadOnlyList<ExpenseHistoryResponse>> result = await _service.GetHistoryAsync(
            user,
            expense.Id,
            CancellationToken.None);

        Assert.AreEqual(ServiceErrorType.Forbidden, result.Error?.Type);
    }

    private Expense SeedApproved(string ownerId)
    {
        Expense expense = _repository.Seed(ExpenseFactory.Draft(ownerId, _repository.Category(2), _clock));
        expense.Submit(ownerId, _clock.UtcNow);
        expense.Approve("bruno", _clock.UtcNow);
        return expense;
    }

    private Expense SeedPaidWithDistinctTimes(string ownerId)
    {
        Expense expense = _repository.Seed(ExpenseFactory.Draft(ownerId, _repository.Category(2), _clock));
        expense.Submit(ownerId, _clock.UtcNow.AddMinutes(1));
        expense.Approve("bruno", _clock.UtcNow.AddMinutes(2));
        expense.Pay("carla", _clock.UtcNow.AddMinutes(3));
        return expense;
    }
}
