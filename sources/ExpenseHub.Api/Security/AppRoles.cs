using System;
using System.Collections.Generic;
using System.Linq;

namespace ExpenseHub.Api.Security;

/// <summary>
/// Roles conhecidas pela aplicação.
/// </summary>
public static class AppRoles
{
    /// <summary>Administra usuários e roles.</summary>
    public const string Admin = "Admin";

    /// <summary>Cria, edita, envia e consulta os próprios reembolsos.</summary>
    public const string Employee = "Employee";

    /// <summary>Aprova ou reprova reembolsos enviados por outras pessoas.</summary>
    public const string Approver = "Approver";

    /// <summary>Registra o pagamento de reembolsos aprovados de outras pessoas.</summary>
    public const string Finance = "Finance";

    /// <summary>Consulta todos os reembolsos e históricos, sem escrita.</summary>
    public const string Auditor = "Auditor";

    /// <summary>Todas as roles que devem existir para o funcionamento da aplicação.</summary>
    public static IReadOnlyList<string> All { get; } = [Admin, Employee, Approver, Finance, Auditor];

    /// <summary>
    /// Procura uma role conhecida ignorando maiúsculas e minúsculas.
    /// </summary>
    /// <param name="role">Nome informado.</param>
    /// <returns>O nome canônico da role ou <see langword="null"/> quando ela não existe.</returns>
    public static string? FindKnown(string? role)
    {
        return All.FirstOrDefault(known => string.Equals(known, role?.Trim(), StringComparison.OrdinalIgnoreCase));
    }
}
