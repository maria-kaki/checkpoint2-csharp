using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Data;
using ExpenseHub.Api.Responses;
using ExpenseHub.Api.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ExpenseHub.Api.Services;

/// <summary>
/// Consulta de usuários e administração de roles, restritas ao Admin.
/// </summary>
public sealed class UserAdministrationService
{
    private readonly AppDbContext _dbContext;
    private readonly UserManager<IdentityUser> _userManager;

    /// <summary>
    /// Cria o serviço.
    /// </summary>
    /// <param name="dbContext">Contexto do banco.</param>
    /// <param name="userManager">Gerenciador de usuários do Identity.</param>
    public UserAdministrationService(AppDbContext dbContext, UserManager<IdentityUser> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    /// <summary>
    /// Lista os usuários com suas roles em uma única consulta.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Usuários ordenados por e-mail.</returns>
    public async Task<IReadOnlyList<UserResponse>> ListAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .OrderBy(user => user.Email)
            .Select(user => new UserResponse(
                user.Id,
                user.Email ?? string.Empty,
                _dbContext.UserRoles
                    .Where(userRole => userRole.UserId == user.Id)
                    .Join(_dbContext.Roles, userRole => userRole.RoleId, role => role.Id, (userRole, role) => role.Name ?? string.Empty)
                    .OrderBy(name => name)
                    .ToList()))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Substitui as roles de um usuário pelo conjunto informado e invalida a sessão atual dele,
    /// exigindo novo login para que o token reflita as novas roles.
    /// </summary>
    /// <param name="actorId">Admin que executa a operação.</param>
    /// <param name="userId">Usuário alterado.</param>
    /// <param name="requestedRoles">Conjunto completo de roles desejado.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O usuário com as roles finais ou a falha correspondente.</returns>
    public async Task<ServiceResult<UserResponse>> UpdateRolesAsync(
        string actorId,
        string userId,
        IReadOnlyList<string?> requestedRoles,
        CancellationToken cancellationToken)
    {
        IdentityUser? user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return ServiceResult.Failure<UserResponse>(ServiceError.NotFound("Usuário não encontrado."));
        }

        IList<string> currentRoles = await _userManager.GetRolesAsync(user);
        RoleChangePlan plan = RoleChangePlan.Create(currentRoles, requestedRoles, isSelf: user.Id == actorId);

        if (plan.UnknownRoles.Count > 0)
        {
            return ServiceResult.Failure<UserResponse>(ServiceError.Validation(
                "roles",
                $"Roles desconhecidas: {string.Join(", ", plan.UnknownRoles)}. Válidas: {string.Join(", ", AppRoles.All)}."));
        }

        if (plan.RemovesOwnAdminRole)
        {
            return ServiceResult.Failure<UserResponse>(
                ServiceError.Forbidden("O Admin não pode remover a própria role Admin."));
        }

        if (plan.HasChanges)
        {
            await ApplyAsync(user, plan, cancellationToken);
        }

        return ServiceResult.Success(new UserResponse(user.Id, user.Email ?? string.Empty, plan.FinalRoles));
    }

    private async Task ApplyAsync(IdentityUser user, RoleChangePlan plan, CancellationToken cancellationToken)
    {
        await using IDbContextTransaction transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        if (plan.RolesToRemove.Count > 0)
        {
            EnsureSucceeded(await _userManager.RemoveFromRolesAsync(user, plan.RolesToRemove));
        }

        if (plan.RolesToAdd.Count > 0)
        {
            EnsureSucceeded(await _userManager.AddToRolesAsync(user, plan.RolesToAdd));
        }

        EnsureSucceeded(await _userManager.UpdateSecurityStampAsync(user));

        await transaction.CommitAsync(cancellationToken);
    }

    private static void EnsureSucceeded(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            string errors = string.Join("; ", result.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"Falha ao alterar roles: {errors}");
        }
    }
}
