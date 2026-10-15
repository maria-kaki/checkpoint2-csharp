using System;
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

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    /// <summary>
    /// Lista os reembolsos no escopo de leitura do usuário: Employee vê os próprios, Approver vê Submitted,
    /// Finance vê Approved e Paid e Auditor vê todos.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>200 com os reembolsos visíveis.</returns>
    [HttpGet]
    [Authorize(Roles = AppRoles.ExpenseReaders)]
    [ProducesResponseType<IReadOnlyList<ExpenseResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        ServiceResult<IReadOnlyList<ExpenseResponse>> result = await _service.ListAsync(User.ToCurrentUser(), cancellationToken);
        return result.Succeeded ? Ok(result.Value) : ToProblem(result.Error!);
    }

    /// <summary>
    /// Consulta um reembolso visível para o usuário.
    /// </summary>
    /// <param name="id">Identificador do reembolso.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>200 com o reembolso ou 404 quando inexistente ou fora do escopo.</returns>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = AppRoles.ExpenseReaders)]
    [ProducesResponseType<ExpenseResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        ServiceResult<ExpenseResponse> result = await _service.GetAsync(User.ToCurrentUser(), id, cancellationToken);
        return result.Succeeded ? Ok(result.Value) : ToProblem(result.Error!);
    }

    /// <summary>
    /// Envia um rascunho próprio para aprovação (Draft → Submitted).
    /// </summary>
    /// <param name="id">Identificador do reembolso.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>200 com o reembolso em Submitted.</returns>
    [HttpPost("{id:guid}/submit")]
    [Authorize(Roles = AppRoles.Employee)]
    [ProducesResponseType<ExpenseResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Submit(Guid id, CancellationToken cancellationToken)
    {
        ServiceResult<ExpenseResponse> result = await _service.SubmitAsync(User.ToCurrentUser(), id, cancellationToken);
        return result.Succeeded ? Ok(result.Value) : ToProblem(result.Error!);
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
