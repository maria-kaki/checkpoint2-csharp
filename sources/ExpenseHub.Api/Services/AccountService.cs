using System;
using System.Linq;
using System.Threading.Tasks;
using ExpenseHub.Api.Dtos;
using ExpenseHub.Api.Responses;
using Microsoft.AspNetCore.Identity;

namespace ExpenseHub.Api.Services;

/// <summary>
/// Cadastro público de usuários.
/// </summary>
public sealed class AccountService
{
    private readonly UserManager<IdentityUser> _userManager;

    /// <summary>
    /// Cria o serviço.
    /// </summary>
    /// <param name="userManager">Gerenciador de usuários do Identity.</param>
    public AccountService(UserManager<IdentityUser> userManager)
    {
        _userManager = userManager;
    }

    /// <summary>
    /// Cadastra um usuário sem nenhuma role. Roles só podem ser atribuídas por um Admin.
    /// </summary>
    /// <param name="request">E-mail e senha.</param>
    /// <returns>O usuário criado ou falha de validação.</returns>
    public async Task<ServiceResult<UserResponse>> RegisterAsync(RegisterRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        IdentityUser user = new()
        {
            UserName = request.Email,
            Email = request.Email,
        };

        IdentityResult result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return ServiceResult.Failure<UserResponse>(new ServiceError(
                ServiceErrorType.Validation,
                "Não foi possível cadastrar o usuário.",
                result.Errors
                    .GroupBy(error => error.Code)
                    .ToDictionary(group => group.Key, group => group.Select(error => error.Description).ToArray())));
        }

        return ServiceResult.Success(new UserResponse(user.Id, request.Email, []));
    }
}
