using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ExpenseHub.Api.Dtos;

/// <summary>
/// Conjunto completo de roles desejado para um usuário em <c>PUT /api/admin/users/{id}/roles</c>.
/// Roles ausentes da lista são removidas; uma lista vazia remove todas as roles.
/// </summary>
public sealed class UpdateUserRolesRequest
{
    /// <summary>Roles que o usuário deve possuir.</summary>
    [Required]
    [MaxLength(10)]
    public IReadOnlyList<string?>? Roles { get; init; }
}
