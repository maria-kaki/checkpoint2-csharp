using System.Threading.Tasks;
using ExpenseHub.Api.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using IdentitySignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace ExpenseHub.Api.Controllers;

/// <summary>
/// Endpoints públicos de autenticação.
/// </summary>
[ApiController]
[AllowAnonymous]
public sealed class AuthController : ControllerBase
{
    private readonly SignInManager<IdentityUser> _signInManager;

    /// <summary>
    /// Cria o controller.
    /// </summary>
    /// <param name="signInManager">Gerenciador de login do Identity.</param>
    public AuthController(SignInManager<IdentityUser> signInManager)
    {
        _signInManager = signInManager;
    }

    /// <summary>
    /// Autentica o usuário e emite um bearer token com as roles atuais.
    /// </summary>
    /// <param name="request">Credenciais.</param>
    /// <returns>Token de acesso ou 401 para credenciais inválidas.</returns>
    [HttpPost("/login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        _signInManager.AuthenticationScheme = IdentityConstants.BearerScheme;

        IdentitySignInResult result = await _signInManager.PasswordSignInAsync(
            request.Email,
            request.Password,
            isPersistent: false,
            lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Credenciais inválidas.");
        }

        // O handler de bearer token já escreveu o token na resposta.
        return new EmptyResult();
    }
}
