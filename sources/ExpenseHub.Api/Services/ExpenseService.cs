using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Domain;
using ExpenseHub.Api.Dtos;
using ExpenseHub.Api.Responses;
using ExpenseHub.Api.Security;

namespace ExpenseHub.Api.Services;

/// <summary>
/// Regras de negócio dos reembolsos. Combina role, ownership e estado antes de alterar dados.
/// </summary>
public sealed class ExpenseService
{
    private readonly IExpenseRepository _repository;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Cria o serviço.
    /// </summary>
    /// <param name="repository">Persistência de reembolsos.</param>
    /// <param name="timeProvider">Relógio do servidor.</param>
    public ExpenseService(IExpenseRepository repository, TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Cria um rascunho cujo proprietário é o usuário autenticado.
    /// </summary>
    /// <param name="user">Usuário autenticado.</param>
    /// <param name="request">Dados do rascunho.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O reembolso criado em Draft ou a falha correspondente.</returns>
    public async Task<ServiceResult<ExpenseResponse>> CreateAsync(
        CurrentUser user,
        ExpenseDraftRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(request);

        if (!user.IsInRole(AppRoles.Employee))
        {
            return ServiceResult.Failure<ExpenseResponse>(
                ServiceError.Forbidden("Somente usuários com a role Employee criam reembolsos."));
        }

        DateTime nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        (ExpenseCategory? category, ServiceError? error) = await ValidateAsync(request, nowUtc, cancellationToken);
        if (error is not null)
        {
            return ServiceResult.Failure<ExpenseResponse>(error);
        }

        Expense expense = Expense.Create(
            user.Id,
            category!,
            request.Description!,
            request.Amount!.Value,
            request.ExpenseDate!.Value,
            nowUtc);

        _repository.Add(expense);
        await _repository.SaveChangesAsync(cancellationToken);

        return ServiceResult.Success(ExpenseResponse.FromEntity(expense));
    }

    /// <summary>
    /// Edita um rascunho próprio. Reembolso invisível retorna 404, de outro proprietário retorna 403
    /// e fora de Draft retorna 409.
    /// </summary>
    /// <param name="user">Usuário autenticado.</param>
    /// <param name="id">Identificador do reembolso.</param>
    /// <param name="request">Novos dados do rascunho.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O reembolso atualizado ou a falha correspondente.</returns>
    public async Task<ServiceResult<ExpenseResponse>> UpdateAsync(
        CurrentUser user,
        Guid id,
        ExpenseDraftRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(request);

        if (!user.IsInRole(AppRoles.Employee))
        {
            return ServiceResult.Failure<ExpenseResponse>(
                ServiceError.Forbidden("Somente usuários com a role Employee editam reembolsos."));
        }

        Expense? expense = await _repository.FindVisibleAsync(id, user, cancellationToken);
        if (expense is null)
        {
            return ServiceResult.Failure<ExpenseResponse>(ServiceError.NotFound("Reembolso não encontrado."));
        }

        if (expense.OwnerId != user.Id)
        {
            return ServiceResult.Failure<ExpenseResponse>(
                ServiceError.Forbidden("Somente o proprietário pode editar o reembolso."));
        }

        if (expense.Status != ExpenseStatus.Draft)
        {
            return ServiceResult.Failure<ExpenseResponse>(
                ServiceError.Conflict("Somente reembolsos em Draft podem ser editados."));
        }

        DateTime nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        (ExpenseCategory? category, ServiceError? error) = await ValidateAsync(request, nowUtc, cancellationToken);
        if (error is not null)
        {
            return ServiceResult.Failure<ExpenseResponse>(error);
        }

        expense.Update(
            user.Id,
            category!,
            request.Description!,
            request.Amount!.Value,
            request.ExpenseDate!.Value,
            nowUtc);

        if (!await _repository.SaveChangesAsync(cancellationToken))
        {
            return ServiceResult.Failure<ExpenseResponse>(
                ServiceError.Conflict("O reembolso foi alterado por outra operação. Consulte-o novamente."));
        }

        return ServiceResult.Success(ExpenseResponse.FromEntity(expense));
    }

    /// <summary>
    /// Envia um rascunho próprio para aprovação (Draft → Submitted). Reembolso invisível retorna 404,
    /// de outro proprietário retorna 403 e fora de Draft (inclusive envio repetido) retorna 409.
    /// </summary>
    /// <param name="user">Usuário autenticado.</param>
    /// <param name="id">Identificador do reembolso.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O reembolso em Submitted ou a falha correspondente.</returns>
    public async Task<ServiceResult<ExpenseResponse>> SubmitAsync(
        CurrentUser user,
        Guid id,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (!user.IsInRole(AppRoles.Employee))
        {
            return ServiceResult.Failure<ExpenseResponse>(
                ServiceError.Forbidden("Somente usuários com a role Employee enviam reembolsos."));
        }

        Expense? expense = await _repository.FindVisibleAsync(id, user, cancellationToken);
        if (expense is null)
        {
            return ServiceResult.Failure<ExpenseResponse>(ServiceError.NotFound("Reembolso não encontrado."));
        }

        if (expense.OwnerId != user.Id)
        {
            return ServiceResult.Failure<ExpenseResponse>(
                ServiceError.Forbidden("Somente o proprietário pode enviar o reembolso."));
        }

        if (!expense.Submit(user.Id, _timeProvider.GetUtcNow().UtcDateTime))
        {
            return ServiceResult.Failure<ExpenseResponse>(ServiceError.Conflict(
                $"Somente reembolsos em Draft podem ser enviados. Estado atual: {expense.Status}."));
        }

        if (!await _repository.SaveChangesAsync(cancellationToken))
        {
            return ServiceResult.Failure<ExpenseResponse>(
                ServiceError.Conflict("O reembolso foi alterado por outra operação. Consulte-o novamente."));
        }

        return ServiceResult.Success(ExpenseResponse.FromEntity(expense));
    }

    /// <summary>
    /// Consulta um reembolso no escopo de leitura do usuário. Fora do escopo, a resposta é a mesma de um
    /// reembolso inexistente, sem revelar que ele existe.
    /// </summary>
    /// <param name="user">Usuário autenticado.</param>
    /// <param name="id">Identificador do reembolso.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O reembolso ou a falha correspondente.</returns>
    public async Task<ServiceResult<ExpenseResponse>> GetAsync(
        CurrentUser user,
        Guid id,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (!HasReadAccess(user))
        {
            return ServiceResult.Failure<ExpenseResponse>(
                ServiceError.Forbidden("O perfil do usuário não consulta reembolsos."));
        }

        Expense? expense = await _repository.FindVisibleAsync(id, user, cancellationToken);
        return expense is null
            ? ServiceResult.Failure<ExpenseResponse>(ServiceError.NotFound("Reembolso não encontrado."))
            : ServiceResult.Success(ExpenseResponse.FromEntity(expense));
    }

    /// <summary>
    /// Lista os reembolsos no escopo de leitura do usuário.
    /// </summary>
    /// <param name="user">Usuário autenticado.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Os reembolsos visíveis ou falha quando o perfil não consulta reembolsos.</returns>
    public async Task<ServiceResult<IReadOnlyList<ExpenseResponse>>> ListAsync(
        CurrentUser user,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (!HasReadAccess(user))
        {
            return ServiceResult.Failure<IReadOnlyList<ExpenseResponse>>(
                ServiceError.Forbidden("O perfil do usuário não consulta reembolsos."));
        }

        IReadOnlyList<Expense> expenses = await _repository.ListVisibleAsync(user, cancellationToken);
        return ServiceResult.Success<IReadOnlyList<ExpenseResponse>>([.. expenses.Select(ExpenseResponse.FromEntity)]);
    }

    private static bool HasReadAccess(CurrentUser user)
    {
        return user.IsInRole(AppRoles.Employee)
            || user.IsInRole(AppRoles.Approver)
            || user.IsInRole(AppRoles.Finance)
            || user.IsInRole(AppRoles.Auditor);
    }

    private async Task<(ExpenseCategory? Category, ServiceError? Error)> ValidateAsync(
        ExpenseDraftRequest request,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        if (request.CategoryId is null
            || request.Description is null
            || request.Amount is null
            || request.ExpenseDate is null)
        {
            return (null, ServiceError.Validation("request", "Categoria, descrição, valor e data são obrigatórios."));
        }

        Dictionary<string, string[]> errors = new(ExpenseRules.Validate(
            request.Description,
            request.Amount.Value,
            request.ExpenseDate.Value,
            DateOnly.FromDateTime(nowUtc)));

        ExpenseCategory? category = await _repository.FindCategoryAsync(request.CategoryId.Value, cancellationToken);
        if (category is null)
        {
            errors["categoryId"] = ["Categoria inexistente."];
        }

        if (errors.Count > 0)
        {
            return (null, new ServiceError(ServiceErrorType.Validation, "Dados do reembolso inválidos.", errors));
        }

        return (category, null);
    }
}
