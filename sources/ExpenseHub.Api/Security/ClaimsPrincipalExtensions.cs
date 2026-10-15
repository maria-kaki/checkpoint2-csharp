using System;
using System.Linq;
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

    /// <summary>
    /// Converte o usuário autenticado na identidade usada pelos serviços.
    /// </summary>
    /// <param name="principal">Usuário da requisição.</param>
    /// <returns>Identificador e roles do token.</returns>
    public static CurrentUser ToCurrentUser(this ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        return new CurrentUser(
            principal.GetUserId(),
            principal.FindAll(ClaimTypes.Role).Select(claim => claim.Value));
    }
}
