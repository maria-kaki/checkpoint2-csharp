using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ExpenseHub.Api.Domain;

/// <summary>
/// Solicitação de reembolso com um único valor.
/// </summary>
public class Expense
{
    /// <summary>Tamanho mínimo da descrição.</summary>
    public const int DescriptionMinLength = 10;

    /// <summary>Tamanho máximo da descrição.</summary>
    public const int DescriptionMaxLength = 500;

    private Expense()
    {
    }

    /// <summary>Identificador gerado pelo servidor.</summary>
    public Guid Id { get; private set; }

    /// <summary>Identificador do usuário proprietário, obtido do token.</summary>
    public string OwnerId { get; private set; } = string.Empty;

    /// <summary>Identificador da categoria.</summary>
    public int CategoryId { get; private set; }

    /// <summary>Categoria da despesa.</summary>
    public ExpenseCategory? Category { get; private set; }

    /// <summary>Descrição da despesa.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Valor solicitado em reais.</summary>
    public decimal Amount { get; private set; }

    /// <summary>Data em que a despesa ocorreu.</summary>
    public DateOnly ExpenseDate { get; private set; }

    /// <summary>Estado atual, controlado pelo servidor.</summary>
    public ExpenseStatus Status { get; private set; }

    /// <summary>Instante de criação em UTC.</summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>Instante da última alteração em UTC.</summary>
    public DateTime UpdatedAtUtc { get; private set; }

    /// <summary>Token de concorrência otimista, renovado a cada alteração.</summary>
    public Guid ConcurrencyStamp { get; private set; }

    /// <summary>Registro de pagamento, quando pago.</summary>
    public PaymentRecord? Payment { get; private set; }

    /// <summary>Histórico de ações do reembolso.</summary>
    public ICollection<ExpenseHistory> History { get; } = [];

    /// <summary>
    /// Cria um rascunho. Identificador, estado e horários são definidos aqui, nunca pelo cliente.
    /// </summary>
    /// <param name="ownerId">Usuário autenticado que cria o reembolso.</param>
    /// <param name="category">Categoria existente.</param>
    /// <param name="description">Descrição da despesa.</param>
    /// <param name="amount">Valor solicitado.</param>
    /// <param name="expenseDate">Data da despesa.</param>
    /// <param name="nowUtc">Instante atual do servidor em UTC.</param>
    /// <returns>O reembolso em <see cref="ExpenseStatus.Draft"/>, com o histórico de criação.</returns>
    public static Expense Create(
        string ownerId,
        ExpenseCategory category,
        string description,
        decimal amount,
        DateOnly expenseDate,
        DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);
        ArgumentNullException.ThrowIfNull(category);
        EnsureValid(description, amount, expenseDate, nowUtc);

        Expense expense = new()
        {
            Id = Guid.CreateVersion7(),
            OwnerId = ownerId,
            CategoryId = category.Id,
            Category = category,
            Description = description.Trim(),
            Amount = amount,
            ExpenseDate = expenseDate,
            Status = ExpenseStatus.Draft,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc,
            ConcurrencyStamp = Guid.NewGuid(),
        };

        expense.History.Add(ExpenseHistory.Create(
            expense.Id,
            ExpenseAction.Created,
            ownerId,
            nowUtc,
            previousStatus: null,
            ExpenseStatus.Draft));

        return expense;
    }

    /// <summary>
    /// Edita o rascunho e registra no histórico os campos alterados. A verificação de proprietário é
    /// feita pelo serviço; aqui é garantido que somente um <see cref="ExpenseStatus.Draft"/> é editado.
    /// </summary>
    /// <param name="actorId">Usuário que edita.</param>
    /// <param name="category">Nova categoria.</param>
    /// <param name="description">Nova descrição.</param>
    /// <param name="amount">Novo valor.</param>
    /// <param name="expenseDate">Nova data da despesa.</param>
    /// <param name="nowUtc">Instante atual do servidor em UTC.</param>
    /// <returns><see langword="false"/> quando o reembolso não está em Draft; nada é alterado.</returns>
    public bool Update(
        string actorId,
        ExpenseCategory category,
        string description,
        decimal amount,
        DateOnly expenseDate,
        DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);
        ArgumentNullException.ThrowIfNull(category);

        if (Status != ExpenseStatus.Draft)
        {
            return false;
        }

        EnsureValid(description, amount, expenseDate, nowUtc);

        string newDescription = description.Trim();
        List<string> changes = [];
        if (CategoryId != category.Id)
        {
            changes.Add(Describe("categoria", Category?.Name ?? CategoryId.ToString(CultureInfo.InvariantCulture), category.Name));
        }

        if (!string.Equals(Description, newDescription, StringComparison.Ordinal))
        {
            changes.Add(Describe("descrição", Description, newDescription));
        }

        if (Amount != amount)
        {
            changes.Add(Describe(
                "valor",
                Amount.ToString("0.00", CultureInfo.InvariantCulture),
                amount.ToString("0.00", CultureInfo.InvariantCulture)));
        }

        if (ExpenseDate != expenseDate)
        {
            changes.Add(Describe(
                "data",
                ExpenseDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                expenseDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
        }

        if (changes.Count == 0)
        {
            return true;
        }

        CategoryId = category.Id;
        Category = category;
        Description = newDescription;
        Amount = amount;
        ExpenseDate = expenseDate;
        UpdatedAtUtc = nowUtc;
        ConcurrencyStamp = Guid.NewGuid();

        string changeLog = string.Join("; ", changes);
        if (changeLog.Length > ExpenseHistory.ChangesMaxLength)
        {
            changeLog = changeLog[..ExpenseHistory.ChangesMaxLength];
        }

        History.Add(ExpenseHistory.Create(
            Id,
            ExpenseAction.Updated,
            actorId,
            nowUtc,
            ExpenseStatus.Draft,
            ExpenseStatus.Draft,
            changes: changeLog));

        return true;
    }

    /// <summary>
    /// Envia o rascunho para aprovação (<see cref="ExpenseStatus.Draft"/> → <see cref="ExpenseStatus.Submitted"/>).
    /// A verificação de proprietário é feita pelo serviço.
    /// </summary>
    /// <param name="actorId">Usuário que envia.</param>
    /// <param name="nowUtc">Instante atual do servidor em UTC.</param>
    /// <returns><see langword="false"/> quando o reembolso não está em Draft; nada é alterado nem registrado.</returns>
    public bool Submit(string actorId, DateTime nowUtc)
    {
        return TryTransition(ExpenseStatus.Draft, ExpenseStatus.Submitted, ExpenseAction.Submitted, actorId, nowUtc);
    }

    /// <summary>Aprova um reembolso enviado.</summary>
    /// <param name="actorId">Aprovador autenticado.</param>
    /// <param name="nowUtc">Instante atual do servidor em UTC.</param>
    /// <returns><see langword="true"/> quando Submitted foi alterado para Approved.</returns>
    public bool Approve(string actorId, DateTime nowUtc)
    {
        return TryTransition(ExpenseStatus.Submitted, ExpenseStatus.Approved, ExpenseAction.Approved, actorId, nowUtc);
    }

    /// <summary>Reprova um reembolso enviado com justificativa.</summary>
    /// <param name="actorId">Aprovador autenticado.</param>
    /// <param name="justification">Justificativa da reprovação.</param>
    /// <param name="nowUtc">Instante atual do servidor em UTC.</param>
    /// <returns><see langword="true"/> quando Submitted foi alterado para Rejected.</returns>
    public bool Reject(string actorId, string justification, DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(justification);
        string normalizedJustification = justification.Trim();
        if (normalizedJustification.Length is < ExpenseHistory.JustificationMinLength
            or > ExpenseHistory.JustificationMaxLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(justification),
                $"A justificativa deve ter entre {ExpenseHistory.JustificationMinLength} e {ExpenseHistory.JustificationMaxLength} caracteres.");
        }

        return TryTransition(
            ExpenseStatus.Submitted,
            ExpenseStatus.Rejected,
            ExpenseAction.Rejected,
            actorId,
            nowUtc,
            normalizedJustification);
    }

    /// <summary>
    /// Executa uma transição de estado e registra o histórico correspondente. Se o estado atual não for o
    /// esperado, nada é alterado: assim uma transição repetida não duplica o histórico.
    /// </summary>
    /// <param name="expected">Estado exigido para a transição.</param>
    /// <param name="next">Próximo estado.</param>
    /// <param name="action">Ação registrada no histórico.</param>
    /// <param name="actorId">Usuário que executa a ação.</param>
    /// <param name="nowUtc">Instante atual do servidor em UTC.</param>
    /// <param name="justification">Justificativa, quando a ação exigir.</param>
    /// <returns><see langword="true"/> quando a transição foi aplicada.</returns>
    private bool TryTransition(
        ExpenseStatus expected,
        ExpenseStatus next,
        ExpenseAction action,
        string actorId,
        DateTime nowUtc,
        string? justification = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);

        if (Status != expected)
        {
            return false;
        }

        ExpenseStatus previous = Status;
        Status = next;
        UpdatedAtUtc = nowUtc;
        ConcurrencyStamp = Guid.NewGuid();

        History.Add(ExpenseHistory.Create(Id, action, actorId, nowUtc, previous, next, justification));

        return true;
    }

    private static void EnsureValid(string description, decimal amount, DateOnly expenseDate, DateTime nowUtc)
    {
        IReadOnlyDictionary<string, string[]> errors = ExpenseRules.Validate(
            description,
            amount,
            expenseDate,
            DateOnly.FromDateTime(nowUtc));

        if (errors.Count > 0)
        {
            throw new ArgumentException(string.Join(" ", errors.Values.SelectMany(messages => messages)));
        }
    }

    private static string Describe(string field, string before, string after)
    {
        return $"{field}: \"{before}\" → \"{after}\"";
    }
}
