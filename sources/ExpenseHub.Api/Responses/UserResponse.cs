using System.Collections.Generic;

namespace ExpenseHub.Api.Responses;

/// <summary>
/// Usuário retornado pelos endpoints de cadastro e administração.
/// </summary>
/// <param name="Id">Identificador do usuário.</param>
/// <param name="Email">E-mail do usuário.</param>
/// <param name="Roles">Roles atuais do usuário.</param>
public sealed record UserResponse(string Id, string Email, IReadOnlyList<string> Roles);
