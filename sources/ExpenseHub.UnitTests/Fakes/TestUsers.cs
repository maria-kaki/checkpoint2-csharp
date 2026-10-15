using ExpenseHub.Api.Security;

namespace ExpenseHub.UnitTests.Fakes;

/// <summary>
/// Identidades usadas nos testes.
/// </summary>
internal static class TestUsers
{
    public static CurrentUser With(string id, params string[] roles)
    {
        return new CurrentUser(id, roles);
    }

    public static CurrentUser Employee(string id)
    {
        return With(id, AppRoles.Employee);
    }
}
