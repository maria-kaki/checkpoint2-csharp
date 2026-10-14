namespace ExpenseHub.Api.Security;

/// <summary>
/// Configuração da conta Admin criada pelo seed.
/// O e-mail fica em appsettings.json; a senha deve vir de User Secrets ou variável de ambiente.
/// </summary>
public sealed class AdminSeedOptions
{
    /// <summary>Seção de configuração.</summary>
    public const string SectionName = "Seed:Admin";

    /// <summary>E-mail (e nome de usuário) da conta Admin.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Senha inicial da conta Admin. Nunca deve ser versionada.</summary>
    public string Password { get; set; } = string.Empty;
}
