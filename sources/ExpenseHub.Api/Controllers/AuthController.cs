using System.Threading.Tasks;
using ExpenseHub.Api.Dtos;
using ExpenseHub.Api.Responses;
using ExpenseHub.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using IdentitySignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace ExpenseHub.Api.Controllers;

/// <summary>
/// Endpoints públicos de cadastro e autenticação.
/// </summary>
[AllowAnonymous]
public sealed class AuthController : ApiControllerBase
{
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly AccountService _accountService;

    /// <summary>
    /// Cria o controller.
    /// </summary>
    /// <param name="signInManager">Gerenciador de login do Identity.</param>
    /// <param name="accountService">Serviço de cadastro.</param>
    public AuthController(SignInManager<IdentityUser> signInManager, AccountService accountService)
    {
        _signInManager = signInManager;
        _accountService = accountService;
    }

    /// <summary>
    /// Cadastra um usuário sem role. Roles são atribuídas apenas pelo Admin.
    /// </summary>
    /// <param name="request">E-mail e senha.</param>
    /// <returns>201 com o usuário criado ou 400 para entrada inválida.</returns>
    [HttpPost("/register")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        ServiceResult<UserResponse> result = await _accountService.RegisterAsync(request);
        if (!result.Succeeded)
        {
            return ToProblem(result.Error!);
        }

        return StatusCode(StatusCodes.Status201Created, result.Value);
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
