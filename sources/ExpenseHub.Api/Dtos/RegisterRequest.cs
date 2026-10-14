using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ExpenseHub.Api.Dtos;

/// <summary>
/// Dados para <c>POST /register</c>. O cadastro não aceita roles: qualquer campo extra,
/// como <c>roles</c>, é rejeitado com 400.
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class RegisterRequest
{
    /// <summary>E-mail, usado também como nome de usuário.</summary>
    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; init; } = string.Empty;

    /// <summary>Senha, validada pela política do Identity.</summary>
    [Required]
    [StringLength(128, MinimumLength = 6)]
    public string Password { get; init; } = string.Empty;
}
