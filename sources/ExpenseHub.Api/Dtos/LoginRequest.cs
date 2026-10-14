using System.ComponentModel.DataAnnotations;

namespace ExpenseHub.Api.Dtos;

/// <summary>
/// Credenciais para <c>POST /login</c>.
/// </summary>
public sealed class LoginRequest
{
    /// <summary>E-mail cadastrado.</summary>
    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; init; } = string.Empty;

    /// <summary>Senha do usuário.</summary>
    [Required]
    [StringLength(128)]
    public string Password { get; init; } = string.Empty;
}
