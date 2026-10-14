using System;
using System.Collections.Generic;
using ExpenseHub.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseHub.Api.Controllers;

/// <summary>
/// Base dos controllers da API: converte falhas de serviço em respostas <c>ProblemDetails</c>.
/// </summary>
[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>
    /// Converte uma falha de serviço no status HTTP correspondente do contrato.
    /// </summary>
    /// <param name="error">Falha retornada pelo serviço.</param>
    /// <returns>Resposta <c>ProblemDetails</c>.</returns>
    protected IActionResult ToProblem(ServiceError error)
    {
        ArgumentNullException.ThrowIfNull(error);

        if (error.Type == ServiceErrorType.Validation)
        {
            IDictionary<string, string[]> errors = error.Errors is null
                ? new Dictionary<string, string[]>()
                : new Dictionary<string, string[]>(error.Errors);

            return ValidationProblem(new ValidationProblemDetails(errors) { Detail = error.Message });
        }

        int statusCode = error.Type switch
        {
            ServiceErrorType.NotFound => StatusCodes.Status404NotFound,
            ServiceErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ServiceErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError,
        };

        return Problem(statusCode: statusCode, title: error.Message);
    }
}
