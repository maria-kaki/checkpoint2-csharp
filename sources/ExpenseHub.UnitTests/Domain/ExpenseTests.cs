using System;
using System.Linq;
using ExpenseHub.Api.Domain;
using ExpenseHub.UnitTests.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Domain;

/// <summary>
/// Criação e edição de rascunhos no domínio.
/// </summary>
[TestClass]
public sealed class ExpenseTests
{
    private static readonly ExpenseCategory _transport = new(2, "Transporte");
    private static readonly ExpenseCategory _lodging = new(3, "Hospedagem");

    private readonly FixedTimeProvider _clock = new();

    /// <summary>A criação define estado, proprietário, identificador e horários pelo servidor.</summary>
    [TestMethod]
    public void Create_DefineDraftProprietarioEHorariosDoServidor()
    {
        Expense expense = ExpenseFactory.Draft("ana", _transport, _clock);

        Assert.AreEqual(ExpenseStatus.Draft, expense.Status);
        Assert.AreEqual("ana", expense.OwnerId);
        Assert.AreNotEqual(Guid.Empty, expense.Id);
        Assert.AreEqual(_clock.UtcNow, expense.CreatedAtUtc);
        Assert.AreEqual(_clock.UtcNow, expense.UpdatedAtUtc);
    }

    /// <summary>A criação registra o histórico com ator, horário e estados.</summary>
    [TestMethod]
    public void Create_RegistraHistoricoDeCriacao()
    {
        Expense expense = ExpenseFactory.Draft("ana", _transport, _clock);

        ExpenseHistory history = expense.History.Single();
        Assert.AreEqual(ExpenseAction.Created, history.Action);
        Assert.AreEqual("ana", history.ActorId);
        Assert.AreEqual(_clock.UtcNow, history.OccurredAtUtc);
        Assert.IsNull(history.PreviousStatus);
        Assert.AreEqual(ExpenseStatus.Draft, history.NewStatus);
        Assert.AreEqual(expense.Id, history.ExpenseId);
    }

    /// <summary>O domínio não aceita dados que violam o contrato, mesmo que o serviço seja contornado.</summary>
    [TestMethod]
    public void Create_ComDataFutura_LancaExcecao()
    {
        Assert.ThrowsExactly<ArgumentException>(() => Expense.Create(
            "ana",
            _transport,
            ExpenseFactory.ValidDescription,
            10m,
            _clock.Today.AddDays(1),
            _clock.UtcNow));
    }

    /// <summary>A edição de um Draft altera os dados e registra os campos alterados com valores antigos e novos.</summary>
    [TestMethod]
    public void Update_EmDraft_AlteraDadosERegistraAlteracoes()
    {
        Expense expense = ExpenseFactory.Draft("ana", _transport, _clock);
        DateTime later = _clock.UtcNow.AddHours(1);

        bool updated = expense.Update("ana", _lodging, ExpenseFactory.ValidDescription, 120m, _clock.Today, later);

        Assert.IsTrue(updated);
        Assert.AreEqual(_lodging.Id, expense.CategoryId);
        Assert.AreEqual(120m, expense.Amount);
        Assert.AreEqual(ExpenseStatus.Draft, expense.Status);
        Assert.AreEqual(later, expense.UpdatedAtUtc);

        ExpenseHistory history = expense.History.Single(item => item.Action == ExpenseAction.Updated);
        Assert.AreEqual("ana", history.ActorId);
        Assert.AreEqual(later, history.OccurredAtUtc);
        Assert.AreEqual(ExpenseStatus.Draft, history.PreviousStatus);
        Assert.AreEqual(ExpenseStatus.Draft, history.NewStatus);
        Assert.Contains("categoria: \"Transporte\" → \"Hospedagem\"", history.Changes!);
        Assert.Contains("valor: \"87.50\" → \"120.00\"", history.Changes!);
        Assert.DoesNotContain("descrição", history.Changes!);
    }

    /// <summary>Uma edição sem mudanças não gera histórico nem altera o token de concorrência.</summary>
    [TestMethod]
    public void Update_SemMudancas_NaoGeraHistorico()
    {
        Expense expense = ExpenseFactory.Draft("ana", _transport, _clock);
        Guid stamp = expense.ConcurrencyStamp;

        bool updated = expense.Update("ana", _transport, ExpenseFactory.ValidDescription, 87.50m, _clock.Today, _clock.UtcNow);

        Assert.IsTrue(updated);
        Assert.HasCount(1, expense.History);
        Assert.AreEqual(stamp, expense.ConcurrencyStamp);
    }
}
