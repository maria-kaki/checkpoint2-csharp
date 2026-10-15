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

    /// <summary>O envio muda Draft para Submitted e registra ator, horário e estados.</summary>
    [TestMethod]
    public void Submit_EmDraft_MudaParaSubmittedERegistraHistorico()
    {
        Expense expense = ExpenseFactory.Draft("ana", _transport, _clock);
        Guid stamp = expense.ConcurrencyStamp;
        DateTime later = _clock.UtcNow.AddMinutes(10);

        bool submitted = expense.Submit("ana", later);

        Assert.IsTrue(submitted);
        Assert.AreEqual(ExpenseStatus.Submitted, expense.Status);
        Assert.AreEqual(later, expense.UpdatedAtUtc);
        Assert.AreNotEqual(stamp, expense.ConcurrencyStamp);

        ExpenseHistory history = expense.History.Single(item => item.Action == ExpenseAction.Submitted);
        Assert.AreEqual("ana", history.ActorId);
        Assert.AreEqual(later, history.OccurredAtUtc);
        Assert.AreEqual(ExpenseStatus.Draft, history.PreviousStatus);
        Assert.AreEqual(ExpenseStatus.Submitted, history.NewStatus);
    }

    /// <summary>Repetir o envio não altera o estado nem duplica o histórico.</summary>
    [TestMethod]
    public void Submit_Repetido_RetornaFalseSemDuplicarHistorico()
    {
        Expense expense = ExpenseFactory.Draft("ana", _transport, _clock);
        expense.Submit("ana", _clock.UtcNow);

        bool submittedAgain = expense.Submit("ana", _clock.UtcNow.AddMinutes(1));

        Assert.IsFalse(submittedAgain);
        Assert.AreEqual(ExpenseStatus.Submitted, expense.Status);
        Assert.AreEqual(1, expense.History.Count(item => item.Action == ExpenseAction.Submitted));
    }

    /// <summary>A aprovação muda Submitted para Approved e registra ator, horário e estados.</summary>
    [TestMethod]
    public void Approve_EmSubmitted_MudaParaApprovedERegistraHistorico()
    {
        Expense expense = ExpenseFactory.Draft("ana", _transport, _clock);
        expense.Submit("ana", _clock.UtcNow);
        DateTime later = _clock.UtcNow.AddHours(1);

        bool approved = expense.Approve("bruno", later);

        Assert.IsTrue(approved);
        Assert.AreEqual(ExpenseStatus.Approved, expense.Status);
        Assert.AreEqual(later, expense.UpdatedAtUtc);

        ExpenseHistory history = expense.History.Single(item => item.Action == ExpenseAction.Approved);
        Assert.AreEqual("bruno", history.ActorId);
        Assert.AreEqual(later, history.OccurredAtUtc);
        Assert.AreEqual(ExpenseStatus.Submitted, history.PreviousStatus);
        Assert.AreEqual(ExpenseStatus.Approved, history.NewStatus);
    }

    /// <summary>A reprovação muda Submitted para Rejected e persiste a justificativa normalizada.</summary>
    [TestMethod]
    public void Reject_EmSubmitted_MudaParaRejectedERegistraJustificativa()
    {
        Expense expense = ExpenseFactory.Draft("ana", _transport, _clock);
        expense.Submit("ana", _clock.UtcNow);
        DateTime later = _clock.UtcNow.AddHours(1);

        bool rejected = expense.Reject("bruno", "  Documento fiscal ilegível.  ", later);

        Assert.IsTrue(rejected);
        Assert.AreEqual(ExpenseStatus.Rejected, expense.Status);

        ExpenseHistory history = expense.History.Single(item => item.Action == ExpenseAction.Rejected);
        Assert.AreEqual("bruno", history.ActorId);
        Assert.AreEqual(later, history.OccurredAtUtc);
        Assert.AreEqual(ExpenseStatus.Submitted, history.PreviousStatus);
        Assert.AreEqual(ExpenseStatus.Rejected, history.NewStatus);
        Assert.AreEqual("Documento fiscal ilegível.", history.Justification);
    }

    /// <summary>Justificativa vazia é recusada pelo domínio sem alterar o reembolso.</summary>
    [TestMethod]
    public void Reject_ComJustificativaVazia_LancaExcecaoSemAlterar()
    {
        Expense expense = ExpenseFactory.Draft("ana", _transport, _clock);
        expense.Submit("ana", _clock.UtcNow);
        int historyCount = expense.History.Count;

        Assert.ThrowsExactly<ArgumentException>(() => expense.Reject("bruno", string.Empty, _clock.UtcNow));
        Assert.AreEqual(ExpenseStatus.Submitted, expense.Status);
        Assert.HasCount(historyCount, expense.History);
    }

    /// <summary>Justificativa curta ou acima de 500 caracteres é recusada pelo domínio.</summary>
    /// <param name="length">Quantidade inválida de caracteres.</param>
    [TestMethod]
    [DataRow(5)]
    [DataRow(ExpenseHistory.JustificationMaxLength + 1)]
    public void Reject_ComTamanhoDeJustificativaInvalido_LancaExcecaoSemAlterar(int length)
    {
        Expense expense = ExpenseFactory.Draft("ana", _transport, _clock);
        expense.Submit("ana", _clock.UtcNow);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            expense.Reject("bruno", new string('x', length), _clock.UtcNow));
        Assert.AreEqual(ExpenseStatus.Submitted, expense.Status);
    }

    /// <summary>Uma segunda decisão retorna falso sem duplicar o histórico.</summary>
    [TestMethod]
    public void Approve_Repetido_RetornaFalseSemDuplicarHistorico()
    {
        Expense expense = ExpenseFactory.Draft("ana", _transport, _clock);
        expense.Submit("ana", _clock.UtcNow);
        expense.Approve("bruno", _clock.UtcNow);

        bool approvedAgain = expense.Approve("bruno", _clock.UtcNow.AddMinutes(1));

        Assert.IsFalse(approvedAgain);
        Assert.AreEqual(ExpenseStatus.Approved, expense.Status);
        Assert.AreEqual(1, expense.History.Count(item => item.Action == ExpenseAction.Approved));
    }

    /// <summary>Depois de enviado, o reembolso não pode mais ser editado.</summary>
    [TestMethod]
    public void Update_AposEnvio_RetornaFalseSemAlterar()
    {
        Expense expense = ExpenseFactory.Draft("ana", _transport, _clock);
        expense.Submit("ana", _clock.UtcNow);
        int historyCount = expense.History.Count;

        bool updated = expense.Update("ana", _lodging, ExpenseFactory.ValidDescription, 999m, _clock.Today, _clock.UtcNow);

        Assert.IsFalse(updated);
        Assert.AreEqual(87.50m, expense.Amount);
        Assert.AreEqual(_transport.Id, expense.CategoryId);
        Assert.HasCount(historyCount, expense.History);
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
