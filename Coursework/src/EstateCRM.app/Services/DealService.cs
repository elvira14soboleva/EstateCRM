using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using EstateCRM.App.Data;
using EstateCRM.App.Models;
using Microsoft.EntityFrameworkCore;

namespace EstateCRM.App.Services;

public sealed class DealValidationException : Exception
{
    public DealValidationException(string message) : base(message) { }
}
public sealed class DealRow
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public string Number { get; set; } = "";
    public string Address { get; set; } = "";
    public string ClientLast { get; set; } = "";
    public string ClientFirst { get; set; } = "";
    public string EmployeeLast { get; set; } = "";
    public string EmployeeFirst { get; set; } = "";
    public decimal Amount { get; set; }
    public decimal Percent { get; set; }
    public decimal Commission { get; set; }
    public string ContractFile { get; set; } = "";
    public override string ToString() => $"№{Id} · {Number} · {Date:dd.MM.yyyy} · {Address}";
    public string Header => $"Сделка №{Id} · {Number} · {Date:dd.MM.yyyy}";
    public string Details => $"Объект: {Address}\nКлиент: {ClientLast} {ClientFirst}\nСотрудник: {EmployeeLast} {EmployeeFirst}";
    public string Money => $"Сумма: {Amount:N2} ₽   ·   Комиссия: {Percent:0.##}% / {Commission:N2} ₽";
    public string FileText => $"Путь к договору: {ContractFile}";
}
public sealed class AvailableViewing
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string Address { get; set; } = "";
    public string ClientLast { get; set; } = "";
    public string ClientFirst { get; set; } = "";
    public string Type { get; set; } = "";
    public decimal Percent { get; set; }
    public string? Comment { get; set; }
    public override string ToString() => $"№{Id} · {Date:dd.MM.yyyy} · {Address} · {ClientLast} {ClientFirst} · {Type} ({Percent:0.##}%) · {Comment ?? "нет комментария"}";
}
public sealed class DealService
{
    public async Task<List<DealRow>> GetDealsAsync()
    {
        await using var db = Db.Create(); await WorkAccess.RequireAsync(db, WorkAccess.All);
        return await db.Deals.AsNoTracking().OrderByDescending(d => d.DealDate).ThenByDescending(d => d.DealId)
            .Select(d => new DealRow { Id = d.DealId, Date = d.DealDate, Number = d.ContractNumber,
                Address = d.Viewing.Property.Address, ClientLast = d.Viewing.Request.Client.LastName,
                ClientFirst = d.Viewing.Request.Client.FirstName, EmployeeLast = d.Employee.LastName,
                EmployeeFirst = d.Employee.FirstName, Amount = d.Amount, Percent = d.CommissionPercent,
                Commission = d.CommissionAmount, ContractFile = d.ContractFile }).ToListAsync();
    }
    public async Task<List<AvailableViewing>> GetAvailableViewingsAsync()
    {
        await using var db = Db.Create(); await WorkAccess.RequireAsync(db, WorkAccess.All);
        return await db.Viewings.AsNoTracking()
            .Where(v => v.ViewingStatus.StatusName == "Проведён" && v.Deal == null && v.Request.IsActive
                && (v.Property.Status.StatusName == "Активный" || v.Property.Status.StatusName == "Зарезервирован")
                && v.Request.DealTypeId == v.Property.DealTypeId)
            .OrderByDescending(v => v.ViewingDatetime)
            .Select(v => new AvailableViewing { Id = v.ViewingId, Date = v.ViewingDatetime, Address = v.Property.Address,
                ClientLast = v.Request.Client.LastName, ClientFirst = v.Request.Client.FirstName,
                Type = v.Property.DealType.DealTypeName, Percent = v.Property.DealType.CommissionPercent, Comment = v.ResultComment }).ToListAsync();
    }
    public async Task CreateAsync(int viewingId, DateOnly date, string number, string file, decimal amount)
    {
        number = number.Trim(); file = file.Trim();
        if (number.Length == 0 || number.Length > 30) throw new DealValidationException("Номер договора должен содержать от 1 до 30 символов.");
        if (file.Length == 0 || file.Length > 260) throw new DealValidationException("Укажите путь к договору длиной до 260 символов.");
        if (!CommissionCalculator.IsValidAmount(amount))
            throw new DealValidationException("Сумма должна быть больше нуля и содержать не более двух знаков после запятой.");
        await using var db = Db.Create();
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        int employeeId = await WorkAccess.RequireAsync(db, WorkAccess.All);
        var viewing = await db.Viewings.Include(v => v.ViewingStatus).Include(v => v.Request)
            .Include(v => v.Property).ThenInclude(p => p.Status)
            .Include(v => v.Property).ThenInclude(p => p.DealType)
            .SingleOrDefaultAsync(v => v.ViewingId == viewingId);
        if (viewing == null || viewing.ViewingStatus.StatusName != "Проведён") throw new DealValidationException("Просмотр не найден или не проведён.");
        if (date < DateOnly.FromDateTime(viewing.ViewingDatetime)) throw new DealValidationException("Дата сделки не может быть раньше даты просмотра.");
        if (!viewing.Request.IsActive) throw new DealValidationException("Заявка уже закрыта.");
        if (viewing.Property.Status.StatusName != "Активный" && viewing.Property.Status.StatusName != "Зарезервирован")
            throw new DealValidationException("Объект уже недоступен для сделки.");
        if (viewing.Request.DealTypeId != viewing.Property.DealTypeId) throw new DealValidationException("Вид сделки заявки и объекта не совпадает.");
        if (await db.Deals.AnyAsync(d => d.ViewingId == viewingId)) throw new DealValidationException("По этому просмотру сделка уже оформлена.");
        if (await db.Deals.AnyAsync(d => d.ContractNumber == number)) throw new DealValidationException("Договор с таким номером уже существует.");
        decimal percent = viewing.Property.DealType.CommissionPercent;
        if (!CommissionCalculator.IsValidPercent(percent)) throw new DealValidationException("Недопустимый процент комиссии в справочнике.");
        var commission = CommissionCalculator.Calculate(amount, percent);
        if (commission > 999999999999.99m) throw new DealValidationException("Сумма комиссии превышает допустимое значение.");
        string finalStatus = viewing.Property.DealType.DealTypeName switch { "Продажа" => "Продан", "Аренда" => "Сдан в аренду", _ => throw new DealValidationException("Неизвестный вид сделки.") };
        var status = await db.PropertyStatuses.SingleOrDefaultAsync(s => s.StatusName == finalStatus)
            ?? throw new DealValidationException("Не найден нужный статус объекта в справочнике.");
        db.Deals.Add(new Deal { ViewingId = viewingId, EmployeeId = employeeId, DealDate = date, ContractNumber = number,
            ContractFile = file, Amount = amount, CommissionPercent = percent, CommissionAmount = commission });
        viewing.Property.StatusId = status.StatusId;
        viewing.Request.IsActive = false;
        await db.SaveChangesAsync(); await transaction.CommitAsync();
    }
    public async Task ChangeDateAsync(int dealId, DateOnly date)
    {
        await using var db = Db.Create(); await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        await WorkAccess.RequireAsync(db, WorkAccess.Admin);
        var deal = await db.Deals.Include(d => d.Viewing).SingleOrDefaultAsync(d => d.DealId == dealId)
            ?? throw new DealValidationException("Сделка не найдена. Обновите список.");
        if (date < DateOnly.FromDateTime(deal.Viewing.ViewingDatetime)) throw new DealValidationException("Дата сделки не может быть раньше даты просмотра.");
        deal.DealDate = date; await db.SaveChangesAsync(); await tx.CommitAsync();
    }
}
