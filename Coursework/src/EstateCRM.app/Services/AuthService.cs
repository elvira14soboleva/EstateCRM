using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using EstateCRM.App.Data;
using Microsoft.EntityFrameworkCore;

namespace EstateCRM.App.Services;

public sealed class AuthService
{
    private readonly SemaphoreSlim _signInGate = new(1, 1);
    private readonly LoginAttemptLimiter _attempts = new();

    public async Task<bool> SignInAsync(string login, string password)
    {
        await _signInGate.WaitAsync();
        try
        {
            Session.Clear();
            _attempts.EnsureAllowed();
            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrEmpty(password))
                return false; 

            await using var db = Db.Create();
            var employee = await db.Employees
                .AsNoTracking()
                .Include(e => e.Role)
                .SingleOrDefaultAsync(e => e.Login == login.Trim());

            if (employee is null || !employee.IsActive || !VerifyPassword(password, employee.PasswordHash))
            {
                _attempts.RecordFailure();
                return false;
            }

            _attempts.RecordSuccess();
            string fullName = string.Join(" ", new[]
            {
                employee.LastName, employee.FirstName, employee.MiddleName
            }.Where(part => !string.IsNullOrWhiteSpace(part)));

            Session.Set(new SignedInEmployee(
                employee.EmployeeId,
                fullName,
                employee.RoleId,
                employee.Role?.RoleName ?? "Роль не указана"));
            return true;
        }
        finally { _signInGate.Release(); }
    }

   
    private static bool VerifyPassword(string password, string stored)
    {
        if (string.IsNullOrWhiteSpace(stored))
            return false;

        string[] parts = stored.Split('.');
        if (parts.Length != 3
            || !int.TryParse(parts[0], out int iterations)
            || iterations < 1
            || iterations > 2_000_000)
            return false;

        byte[]? actual = null;
        try
        {
            byte[] salt = Convert.FromBase64String(parts[1]);
            byte[] expected = Convert.FromBase64String(parts[2]);
            if (salt.Length < 8 || salt.Length > 128
                || expected.Length < 16 || expected.Length > 128)
                return false;

            actual = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                iterations,
                HashAlgorithmName.SHA256,
                expected.Length);

            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            return false;
        }
        finally
        {
            if (actual is not null)
                CryptographicOperations.ZeroMemory(actual);
        }
    }
}
