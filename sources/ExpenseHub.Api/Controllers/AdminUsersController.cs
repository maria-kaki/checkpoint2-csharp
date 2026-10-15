using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Dtos;
using ExpenseHub.Api.Responses;
using ExpenseHub.Api.Security;
using ExpenseHub.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseHub.Api.Controllers;

/// <summary>
/// Administração de usuários e roles. Acesso exclusivo da role Admin.
/// </summary>
[Route("api/admin/users")]
[Authorize(Roles = AppRoles.Admin)]
public sealed class AdminUsersController : ApiControllerBase
{
    private readonly UserAdministrationService _service;

    /// <summary>
    /// Cria o controller.
    /// </summary>
    /// <param name="service">Serviço de administração de usuários.</param>
    public AdminUsersController(UserAdministrationService service)
    {
        _service = service;
    }

    /// <summary>
    /// Lista os usuários e suas roles.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Usuários cadastrados.</returns>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<UserResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        return Ok(await _service.ListAsync(cancellationToken));
    }

    /// <summary>
    /// Substitui as roles de um usuário. O usuário precisa fazer login novamente para receber as novas roles.
    /// </summary>
    /// <param name="id">Identificador do usuário.</param>
    /// <param name="request">Conjunto completo de roles desejado.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O usuário com as roles atualizadas.</returns>
    [HttpPut("{id}/roles")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRoles(
        string id,
        [FromBody] UpdateUserRolesRequest request,
        CancellationToken cancellationToken)
    {
        ServiceResult<UserResponse> result = await _service.UpdateRolesAsync(
            User.GetUserId(),
            id,
            request.Roles!,
            cancellationToken);

        return result.Succeeded ? Ok(result.Value) : ToProblem(result.Error!);
    }
}
