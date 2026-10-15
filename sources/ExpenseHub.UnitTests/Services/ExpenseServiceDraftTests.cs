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
/// Criação e edição de rascunhos no serviço: role, ownership, estado e validação.
/// </summary>
[TestClass]
public sealed class ExpenseServiceDraftTests
{
    private readonly FakeExpenseRepository _repository = new();
    private readonly FixedTimeProvider _clock = new();
    private readonly ExpenseService _service;

    /// <summary>Cria o serviço com repositório em memória e relógio fixo.</summary>
    public ExpenseServiceDraftTests()
    {
        _service = new ExpenseService(_repository, _clock);
    }

    /// <summary>O proprietário vem da identidade autenticada e o estado inicial é Draft.</summary>
    /// <returns>Tarefa assíncrona.</returns>
    [TestMethod]
    public async Task CreateAsync_Employee_CriaDraftComProprietarioDoToken()
    {
        ServiceResult<ExpenseResponse> result = await _service.CreateAsync(
            TestUsers.Employee("ana"),
            ExpenseFactory.Request(),
            CancellationToken.None);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("ana", result.Value!.OwnerId);
        Assert.AreEqual(ExpenseStatus.Draft, result.Value.Status);
        Assert.AreEqual("Transporte", result.Value.CategoryName);
        Assert.AreEqual(_clock.UtcNow, result.Value.CreatedAtUtc);
        Assert.HasCount(1, _repository.Expenses);
        Assert.AreEqual(1, _repository.SaveCount);
    }

    /// <summary>Somente quem possui a role Employee cria reembolsos, inclusive Admin, Approver, Finance e Auditor.</summary>
    /// <param name="role">Role sem permissão de criação.</param>
    /// <returns>Tarefa assíncrona.</returns>
    [TestMethod]
    [DataRow(AppRoles.Admin)]
    [DataRow(AppRoles.Approver)]
    [DataRow(AppRoles.Finance)]
    [DataRow(AppRoles.Auditor)]
    public async Task CreateAsync_SemRoleEmployee_RetornaForbidden(string role)
    {
        ServiceResult<ExpenseResponse> result = await _service.CreateAsync(
            TestUsers.With("ana", role),
            ExpenseFactory.Request(),
            CancellationToken.None);

        Assert.AreEqual(ServiceErrorType.Forbidden, result.Error?.Type);
        Assert.IsEmpty(_repository.Expenses);
    }

    /// <summary>Data futura é rejeitada sem persistir nada.</summary>
    /// <returns>Tarefa assíncrona.</returns>
    [TestMethod]
    public async Task CreateAsync_DataFutura_RetornaValidacaoSemPersistir()
    {
        ServiceResult<ExpenseResponse> result = await _service.CreateAsync(
            TestUsers.Employee("ana"),
            ExpenseFactory.Request(expenseDate: _clock.Today.AddDays(1)),
            CancellationToken.None);

        Assert.AreEqual(ServiceErrorType.Validation, result.Error?.Type);
        Assert.IsTrue(result.Error!.Errors!.ContainsKey("expenseDate"));
        Assert.IsEmpty(_repository.Expenses);
        Assert.AreEqual(0, _repository.SaveCount);
    }

    /// <summary>Categoria inexistente é rejeitada.</summary>
    /// <returns>Tarefa assíncrona.</returns>
    [TestMethod]
    public async Task CreateAsync_CategoriaInexistente_RetornaValidacao()
    {
        ServiceResult<ExpenseResponse> result = await _service.CreateAsync(
            TestUsers.Employee("ana"),
            ExpenseFactory.Request(categoryId: 99),
            CancellationToken.None);

        Assert.AreEqual(ServiceErrorType.Validation, result.Error?.Type);
        Assert.IsTrue(result.Error!.Errors!.ContainsKey("categoryId"));
    }

    /// <summary>O proprietário edita o próprio Draft e a alteração é registrada no histórico.</summary>
    /// <returns>Tarefa assíncrona.</returns>
    [TestMethod]
    public async Task UpdateAsync_ProprietarioEmDraft_AtualizaERegistraHistorico()
    {
        Expense expense = _repository.Seed(ExpenseFactory.Draft("ana", _repository.Category(2), _clock));

        ServiceResult<ExpenseResponse> result = await _service.UpdateAsync(
            TestUsers.Employee("ana"),
            expense.Id,
            ExpenseFactory.Request(categoryId: 3, amount: 450m),
            CancellationToken.None);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual(450m, result.Value!.Amount);
        Assert.AreEqual("Hospedagem", result.Value.CategoryName);
        Assert.AreEqual(1, expense.History.Count(history => history.Action == ExpenseAction.Updated));
        Assert.AreEqual(1, _repository.SaveCount);
    }

    /// <summary>Outro Employee não enxerga o rascunho: a resposta é 404 e nada é alterado.</summary>
    /// <returns>Tarefa assíncrona.</returns>
    [TestMethod]
    public async Task UpdateAsync_OutroEmployee_RetornaNotFoundSemAlterar()
    {
        Expense expense = _repository.Seed(ExpenseFactory.Draft("ana", _repository.Category(2), _clock));

        ServiceResult<ExpenseResponse> result = await _service.UpdateAsync(
            TestUsers.Employee("bruno"),
            expense.Id,
            ExpenseFactory.Request(amount: 1m),
            CancellationToken.None);

        Assert.AreEqual(ServiceErrorType.NotFound, result.Error?.Type);
        Assert.AreEqual(87.50m, expense.Amount);
        Assert.AreEqual(0, _repository.SaveCount);
    }

    /// <summary>Um Auditor que também é Employee enxerga o rascunho alheio, mas não pode editá-lo (403).</summary>
    /// <returns>Tarefa assíncrona.</returns>
    [TestMethod]
    public async Task UpdateAsync_NaoProprietarioQueEnxergaODraft_RetornaForbidden()
    {
        Expense expense = _repository.Seed(ExpenseFactory.Draft("ana", _repository.Category(2), _clock));

        ServiceResult<ExpenseResponse> result = await _service.UpdateAsync(
            TestUsers.With("carla", AppRoles.Employee, AppRoles.Auditor),
            expense.Id,
            ExpenseFactory.Request(amount: 1m),
            CancellationToken.None);

        Assert.AreEqual(ServiceErrorType.Forbidden, result.Error?.Type);
        Assert.AreEqual(87.50m, expense.Amount);
    }

    /// <summary>Reembolso inexistente retorna 404.</summary>
    /// <returns>Tarefa assíncrona.</returns>
    [TestMethod]
    public async Task UpdateAsync_Inexistente_RetornaNotFound()
    {
        ServiceResult<ExpenseResponse> result = await _service.UpdateAsync(
            TestUsers.Employee("ana"),
            Guid.NewGuid(),
            ExpenseFactory.Request(),
            CancellationToken.None);

        Assert.AreEqual(ServiceErrorType.NotFound, result.Error?.Type);
    }

    /// <summary>Dados inválidos na edição são rejeitados sem alterar o rascunho.</summary>
    /// <returns>Tarefa assíncrona.</returns>
    [TestMethod]
    public async Task UpdateAsync_DadosInvalidos_RetornaValidacaoSemAlterar()
    {
        Expense expense = _repository.Seed(ExpenseFactory.Draft("ana", _repository.Category(2), _clock));

        ServiceResult<ExpenseResponse> result = await _service.UpdateAsync(
            TestUsers.Employee("ana"),
            expense.Id,
            ExpenseFactory.Request(amount: 0m),
            CancellationToken.None);

        Assert.AreEqual(ServiceErrorType.Validation, result.Error?.Type);
        Assert.AreEqual(87.50m, expense.Amount);
        Assert.HasCount(1, expense.History);
    }

    /// <summary>Se outra operação alterou o reembolso ao mesmo tempo, a edição retorna conflito.</summary>
    /// <returns>Tarefa assíncrona.</returns>
    [TestMethod]
    public async Task UpdateAsync_ConflitoDeConcorrencia_RetornaConflict()
    {
        Expense expense = _repository.Seed(ExpenseFactory.Draft("ana", _repository.Category(2), _clock));
        _repository.SimulateConcurrencyConflict = true;

        ServiceResult<ExpenseResponse> result = await _service.UpdateAsync(
            TestUsers.Employee("ana"),
            expense.Id,
            ExpenseFactory.Request(amount: 10m),
            CancellationToken.None);

        Assert.AreEqual(ServiceErrorType.Conflict, result.Error?.Type);
    }
}
