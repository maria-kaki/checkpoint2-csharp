using System;
using System.Security.Claims;

namespace ExpenseHub.Api.Security;

/// <summary>
/// Leitura da identidade autenticada a partir das claims do token.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Obtém o identificador do usuário autenticado.
    /// </summary>
    /// <param name="principal">Usuário da requisição.</param>
    /// <returns>Identificador do usuário.</returns>
    /// <exception cref="InvalidOperationException">Quando a requisição não está autenticada.</exception>
    public static string GetUserId(this ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        return principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Usuário autenticado sem identificador.");
    }
}
