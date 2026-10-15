using System;
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
/// Reembolsos. Os atributos de role restringem a rota; ownership e estado são verificados no serviço.
/// </summary>
[Route("api/expenses")]
public sealed class ExpensesController : ApiControllerBase
{
    private readonly ExpenseService _service;

    /// <summary>
    /// Cria o controller.
    /// </summary>
    /// <param name="service">Serviço de reembolsos.</param>
    public ExpensesController(ExpenseService service)
    {
        _service = service;
    }

    /// <summary>
    /// Cria um rascunho. O proprietário é o usuário autenticado.
    /// </summary>
    /// <param name="request">Dados do rascunho.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>201 com o reembolso em Draft.</returns>
    [HttpPost]
    [Authorize(Roles = AppRoles.Employee)]
    [ProducesResponseType<ExpenseResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] ExpenseDraftRequest request, CancellationToken cancellationToken)
    {
        ServiceResult<ExpenseResponse> result = await _service.CreateAsync(User.ToCurrentUser(), request, cancellationToken);
        if (!result.Succeeded)
        {
            return ToProblem(result.Error!);
        }

        return Created(new Uri($"/api/expenses/{result.Value!.Id}", UriKind.Relative), result.Value);
    }

    /// <summary>
    /// Edita um rascunho próprio.
    /// </summary>
    /// <param name="id">Identificador do reembolso.</param>
    /// <param name="request">Novos dados do rascunho.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>200 com o reembolso atualizado.</returns>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = AppRoles.Employee)]
    [ProducesResponseType<ExpenseResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] ExpenseDraftRequest request,
        CancellationToken cancellationToken)
    {
        ServiceResult<ExpenseResponse> result = await _service.UpdateAsync(User.ToCurrentUser(), id, request, cancellationToken);
        return result.Succeeded ? Ok(result.Value) : ToProblem(result.Error!);
    }
}
