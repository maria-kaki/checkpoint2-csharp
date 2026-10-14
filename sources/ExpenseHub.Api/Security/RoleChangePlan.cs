using System;
using System.Collections.Generic;
using System.Linq;

namespace ExpenseHub.Api.Security;

/// <summary>
/// Calcula a alteração de roles de um usuário a partir do conjunto desejado e aplica as regras:
/// somente roles conhecidas e nenhum Admin remove a própria role Admin.
/// </summary>
public sealed class RoleChangePlan
{
    private RoleChangePlan(
        IReadOnlyList<string> unknownRoles,
        bool removesOwnAdminRole,
        IReadOnlyList<string> finalRoles,
        IReadOnlyList<string> rolesToAdd,
        IReadOnlyList<string> rolesToRemove)
    {
        UnknownRoles = unknownRoles;
        RemovesOwnAdminRole = removesOwnAdminRole;
        FinalRoles = finalRoles;
        RolesToAdd = rolesToAdd;
        RolesToRemove = rolesToRemove;
    }

    /// <summary>Valores informados que não correspondem a nenhuma role conhecida.</summary>
    public IReadOnlyList<string> UnknownRoles { get; }

    /// <summary>Indica que o Admin tentou remover a própria role Admin.</summary>
    public bool RemovesOwnAdminRole { get; }

    /// <summary>Roles que o usuário terá após a alteração, com nomes canônicos.</summary>
    public IReadOnlyList<string> FinalRoles { get; }

    /// <summary>Roles a adicionar.</summary>
    public IReadOnlyList<string> RolesToAdd { get; }

    /// <summary>Roles a remover.</summary>
    public IReadOnlyList<string> RolesToRemove { get; }

    /// <summary>Indica se não há alteração a aplicar.</summary>
    public bool HasChanges => RolesToAdd.Count > 0 || RolesToRemove.Count > 0;

    /// <summary>
    /// Cria o plano de alteração.
    /// </summary>
    /// <param name="currentRoles">Roles atuais do usuário.</param>
    /// <param name="requestedRoles">Conjunto completo de roles desejado.</param>
    /// <param name="isSelf">Indica se o Admin está alterando a própria conta.</param>
    /// <returns>O plano calculado.</returns>
    public static RoleChangePlan Create(
        IEnumerable<string> currentRoles,
        IEnumerable<string?> requestedRoles,
        bool isSelf)
    {
        ArgumentNullException.ThrowIfNull(currentRoles);
        ArgumentNullException.ThrowIfNull(requestedRoles);

        List<string> unknown = [];
        SortedSet<string> desired = new(StringComparer.Ordinal);
        foreach (string? requested in requestedRoles)
        {
            string? known = AppRoles.FindKnown(requested);
            if (known is null)
            {
                unknown.Add(requested ?? string.Empty);
            }
            else
            {
                desired.Add(known);
            }
        }

        HashSet<string> current = new(currentRoles, StringComparer.Ordinal);
        bool removesOwnAdmin = isSelf
            && current.Contains(AppRoles.Admin)
            && !desired.Contains(AppRoles.Admin);

        return new RoleChangePlan(
            unknown,
            removesOwnAdmin,
            [.. desired],
            [.. desired.Where(role => !current.Contains(role))],
            [.. current.Where(role => !desired.Contains(role)).Order(StringComparer.Ordinal)]);
    }
}
