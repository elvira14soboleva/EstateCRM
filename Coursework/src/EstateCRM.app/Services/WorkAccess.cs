using System;
using System.Linq;
using System.Threading.Tasks;
using EstateCRM.App.Data;
using Microsoft.EntityFrameworkCore;

namespace EstateCRM.App.Services;

public static class WorkAccess
{
    public static async Task<int> RequireAsync(EstateDbContext db, params string[] roles)
    {
        var current = Session.Current ?? throw new DealValidationException("Для работы необходимо войти в систему.");
        var employee = await db.Employees.AsNoTracking()
            .Where(e => e.EmployeeId == current.EmployeeId)
            .Select(e => new { e.IsActive, Role = e.Role.RoleName }).SingleOrDefaultAsync();
        if (employee == null || !employee.IsActive) throw new DealValidationException("Учётная запись не найдена или отключена.");
        if (!roles.Contains(employee.Role)) throw new DealValidationException("У вашей роли нет прав на это действие.");
        if (Session.Current?.EmployeeId != current.EmployeeId) throw new DealValidationException("Сеанс изменился. Войдите снова.");
        return current.EmployeeId;
    }
    public static readonly string[] All = { "Администратор", "Руководитель", "Риелтор" };
    public static readonly string[] Editors = { "Администратор", "Риелтор" };
    public static readonly string[] Reporting = { "Руководитель", "Администратор" };
    public static readonly string[] Admin = { "Администратор" };
}
