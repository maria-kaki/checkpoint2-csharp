using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExpenseHub.Api.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExpenseHub.Api.Data;

/// <summary>
/// Cria as roles obrigatórias e a única conta Admin inicial. Pode ser executado várias vezes
/// sem duplicar roles ou usuários.
/// </summary>
public sealed class IdentitySeeder
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly AdminSeedOptions _adminOptions;
    private readonly ILogger<IdentitySeeder> _logger;

    /// <summary>
    /// Cria o seeder.
    /// </summary>
    /// <param name="roleManager">Gerenciador de roles do Identity.</param>
    /// <param name="userManager">Gerenciador de usuários do Identity.</param>
    /// <param name="adminOptions">Configuração da conta Admin.</param>
    /// <param name="logger">Logger.</param>
    public IdentitySeeder(
        RoleManager<IdentityRole> roleManager,
        UserManager<IdentityUser> userManager,
        IOptions<AdminSeedOptions> adminOptions,
        ILogger<IdentitySeeder> logger)
    {
        ArgumentNullException.ThrowIfNull(adminOptions);

        _roleManager = roleManager;
        _userManager = userManager;
        _adminOptions = adminOptions.Value;
        _logger = logger;
    }

    /// <summary>
    /// Executa o seed de roles e da conta Admin.
    /// </summary>
    /// <returns>Tarefa assíncrona.</returns>
    public async Task SeedAsync()
    {
        await SeedRolesAsync();
        await SeedAdminAsync();
    }

    private async Task SeedRolesAsync()
    {
        foreach (string role in AppRoles.All)
        {
            if (await _roleManager.RoleExistsAsync(role))
            {
                continue;
            }

            IdentityResult result = await _roleManager.CreateAsync(new IdentityRole(role));
            EnsureSucceeded(result, $"criar a role {role}");
        }
    }

    private async Task SeedAdminAsync()
    {
        IList<IdentityUser> admins = await _userManager.GetUsersInRoleAsync(AppRoles.Admin);
        if (admins.Count > 0)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_adminOptions.Email) || string.IsNullOrWhiteSpace(_adminOptions.Password))
        {
            throw new InvalidOperationException(
                "Conta Admin não configurada. Defina Seed:Admin:Email e Seed:Admin:Password "
                + "via User Secrets ou variáveis de ambiente (veja o README).");
        }

        IdentityUser? admin = await _userManager.FindByEmailAsync(_adminOptions.Email);
        if (admin is null)
        {
            admin = new IdentityUser
            {
                UserName = _adminOptions.Email,
                Email = _adminOptions.Email,
                EmailConfirmed = true,
            };

            IdentityResult createResult = await _userManager.CreateAsync(admin, _adminOptions.Password);
            EnsureSucceeded(createResult, "criar a conta Admin");
        }

        IdentityResult roleResult = await _userManager.AddToRoleAsync(admin, AppRoles.Admin);
        EnsureSucceeded(roleResult, "vincular a conta Admin à role Admin");

        _logger.LogInformation("Conta Admin inicial criada.");
    }

    private static void EnsureSucceeded(IdentityResult result, string operation)
    {
        if (result.Succeeded)
        {
            return;
        }

        string errors = string.Join("; ", result.Errors.Select(error => error.Description));
        throw new InvalidOperationException($"Falha ao {operation}: {errors}");
    }
}
