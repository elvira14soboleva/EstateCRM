namespace EstateCRM.App.Services;

// Здесь нет пароля, его хеша или отслеживаемой EF-сущности.
public sealed record SignedInEmployee(
    int EmployeeId,
    string FullName,
    int RoleId,
    string RoleName);

public static class Session
{
    public static SignedInEmployee? Current { get; private set; }
    public static bool IsAuthenticated => Current is not null;
    public static string Caption => Current is null
        ? "Вход не выполнен"
        : $"{Current.FullName} · {Current.RoleName}";

    internal static void Set(SignedInEmployee employee) => Current = employee;
    public static void Clear() => Current = null;
}
