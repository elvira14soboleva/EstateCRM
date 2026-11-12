using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using EstateCRM.App.Data;
using EstateCRM.App.Models;
using Microsoft.EntityFrameworkCore;

namespace EstateCRM.App.Services;

public sealed class RequestOption
{
    public int Id { get; set; }
    public string Client { get; set; } = "";
    public string Type { get; set; } = "";
    public string DealType { get; set; } = "";
    public string? District { get; set; }
    public decimal Min { get; set; }
    public decimal Max { get; set; }
    public decimal? Area { get; set; }
    public byte? Rooms { get; set; }
    public override string ToString() => $"Заявка №{Id} · {Client} · {Type} · {DealType} · {Min:N0}–{Max:N0} ₽";
    public string Conditions => $"Район: {District ?? "любой"}; площадь от: {(Area.HasValue ? Area.Value.ToString("0.##") + " м²" : "любая")}; комнат: {(Rooms.HasValue ? Rooms.Value.ToString() : "любое количество")}.";
}
public sealed class MatchedProperty
{
    public int Id { get; set; }
    public string Address { get; set; } = "";
    public decimal Price { get; set; }
    public decimal Area { get; set; }
    public byte Rooms { get; set; }
    public override string ToString() => $"№{Id} · {Address} · {Price:N2} ₽ · {Area:0.##} м² · комнат: {Rooms}";
}
public sealed class ViewingRow
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string Status { get; set; } = "";
    public string Address { get; set; } = "";
    public string ClientLast { get; set; } = "";
    public string ClientFirst { get; set; } = "";
    public string? Comment { get; set; }
    public bool HasDeal { get; set; }
    public string Header => $"Просмотр №{Id} · {Date:dd.MM.yyyy HH:mm} · {Status}";
    public string Details => $"{Address}\nКлиент: {ClientLast} {ClientFirst}\nРезультат: {(string.IsNullOrWhiteSpace(Comment) ? "не указан" : Comment)}" + (HasDeal ? "\nОформлена сделка" : "");
    public override string ToString() => $"№{Id} · {Date:dd.MM.yyyy HH:mm} · {Status} · {Address} · {ClientLast} {ClientFirst}";
}
public sealed class ViewingService
{
    public async Task<List<RequestOption>> RequestsAsync()
    {
        await using var db = Db.Create(); await WorkAccess.RequireAsync(db, WorkAccess.All);
        return await db.ClientRequests.AsNoTracking().Where(r => r.IsActive).OrderByDescending(r => r.RequestId)
            .Select(r => new RequestOption { Id = r.RequestId, Client = r.Client.LastName + " " + r.Client.FirstName,
                Type = r.PropertyType.TypeName, DealType = r.DealType.DealTypeName,
                District = r.District == null ? null : r.District.DistrictName, Min = r.PriceMin, Max = r.PriceMax,
                Area = r.AreaMin, Rooms = r.Rooms }).ToListAsync();
    }
    private static IQueryable<Property> Match(EstateDbContext db, ClientRequest r) => db.Properties.AsNoTracking()
        .Where(p => p.Status.StatusName == "Активный" && p.PropertyTypeId == r.PropertyTypeId
            && p.DealTypeId == r.DealTypeId && p.Price >= r.PriceMin && p.Price <= r.PriceMax
            && (r.DistrictId == null || p.DistrictId == r.DistrictId)
            && (r.AreaMin == null || p.Area >= r.AreaMin)
            && (r.Rooms == null || p.Rooms == r.Rooms));
    public async Task<List<MatchedProperty>> MatchesAsync(int requestId)
    {
        await using var db = Db.Create(); await WorkAccess.RequireAsync(db, WorkAccess.All);
        var r = await db.ClientRequests.AsNoTracking().SingleOrDefaultAsync(x => x.RequestId == requestId && x.IsActive)
            ?? throw new DealValidationException("Заявка не найдена или уже закрыта.");
        return await Match(db, r).OrderBy(p => p.Price).ThenBy(p => p.PropertyId)
            .Select(p => new MatchedProperty { Id = p.PropertyId, Address = p.Address, Price = p.Price, Area = p.Area, Rooms = p.Rooms }).ToListAsync();
    }
    public async Task<List<ViewingRow>> ListAsync()
    {
        await using var db = Db.Create(); await WorkAccess.RequireAsync(db, WorkAccess.All);
        return await db.Viewings.AsNoTracking().OrderByDescending(v => v.ViewingDatetime).ThenByDescending(v => v.ViewingId)
            .Select(v => new ViewingRow { Id = v.ViewingId, Date = v.ViewingDatetime, Status = v.ViewingStatus.StatusName,
                Address = v.Property.Address, ClientLast = v.Request.Client.LastName, ClientFirst = v.Request.Client.FirstName,
                Comment = v.ResultComment, HasDeal = v.Deal != null }).ToListAsync();
    }
    public async Task CreateAsync(int requestId, int propertyId, DateTime date)
    {
        if (date < DateTime.Now) throw new DealValidationException("Укажите будущую дату и время просмотра.");
        if (date.Year > 9998) throw new DealValidationException("Недопустимая дата.");
        date = new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, 0);
        await using var db = Db.Create(); await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        await WorkAccess.RequireAsync(db, WorkAccess.All);
        var r = await db.ClientRequests.AsNoTracking().SingleOrDefaultAsync(x => x.RequestId == requestId && x.IsActive)
            ?? throw new DealValidationException("Заявка не найдена или закрыта.");
        if (!await Match(db, r).AnyAsync(p => p.PropertyId == propertyId)) throw new DealValidationException("Объект больше не подходит под условия заявки или недоступен.");
        var from = date.AddMinutes(-30); var to = date.AddMinutes(30);
        if (await db.Viewings.AnyAsync(v => v.PropertyId == propertyId && v.ViewingStatus.StatusName != "Отменён"
            && v.ViewingDatetime > from && v.ViewingDatetime < to))
            throw new DealValidationException("На это время уже назначен просмотр объекта (интервал 30 минут).");
        int status = await db.ViewingStatuses.Where(s => s.StatusName == "Запланирован").Select(s => s.ViewingStatusId).SingleAsync();
        db.Viewings.Add(new Viewing { RequestId = requestId, PropertyId = propertyId, ViewingDatetime = date, ViewingStatusId = status });
        await db.SaveChangesAsync(); await tx.CommitAsync();
    }
    public async Task SetStatusAsync(int id, string newStatus, string comment)
    {
        if (newStatus != "Проведён" && newStatus != "Отменён") throw new DealValidationException("Недопустимый статус.");
        comment = comment.Trim();
        if (comment.Length > 500) throw new DealValidationException("Комментарий не должен превышать 500 символов.");
        await using var db = Db.Create(); await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        await WorkAccess.RequireAsync(db, WorkAccess.All);
        var v = await db.Viewings.Include(x => x.ViewingStatus).SingleOrDefaultAsync(x => x.ViewingId == id)
            ?? throw new DealValidationException("Просмотр не найден.");
        if (v.ViewingStatus.StatusName != "Запланирован" || await db.Deals.AnyAsync(d => d.ViewingId == id))
            throw new DealValidationException("Изменить можно только запланированный просмотр без сделки.");
        var status = await db.ViewingStatuses.SingleOrDefaultAsync(s => s.StatusName == newStatus)
            ?? throw new DealValidationException("Статус просмотра не найден в справочнике.");
        v.ViewingStatusId = status.ViewingStatusId; v.ResultComment = comment.Length == 0 ? null : comment;
        await db.SaveChangesAsync(); await tx.CommitAsync();
    }
}
