using System;
using System.Collections.Generic;

namespace ExpenseHub.Api.Security;

/// <summary>
/// Identidade autenticada usada pelos serviços: identificador e roles extraídos do token,
/// nunca de dados enviados pelo cliente.
/// </summary>
public sealed class CurrentUser
{
    private readonly HashSet<string> _roles;

    /// <summary>
    /// Cria a identidade.
    /// </summary>
    /// <param name="id">Identificador do usuário.</param>
    /// <param name="roles">Roles do usuário.</param>
    public CurrentUser(string id, IEnumerable<string> roles)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(roles);

        Id = id;
        _roles = new HashSet<string>(roles, StringComparer.Ordinal);
    }

    /// <summary>Identificador do usuário.</summary>
    public string Id { get; }

    /// <summary>Roles do usuário.</summary>
    public IReadOnlyCollection<string> Roles => _roles;

    /// <summary>
    /// Verifica se o usuário possui a role.
    /// </summary>
    /// <param name="role">Nome da role.</param>
    /// <returns><see langword="true"/> quando o usuário possui a role.</returns>
    public bool IsInRole(string role)
    {
        return _roles.Contains(role);
    }
}
