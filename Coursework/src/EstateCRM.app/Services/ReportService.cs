using System;
using System.Linq;
using System.Threading.Tasks;
using EstateCRM.App.Data;
using Microsoft.EntityFrameworkCore;

namespace EstateCRM.App.Services;

public sealed class DealSummary
{
    public int Count { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TotalCommission { get; set; }
}

// Только чтение: изменения в таблицы не записываются.
public sealed class ReportService
{
    public static void ValidatePeriod(DateOnly from, DateOnly to)
    {
        if (from > to)
            throw new DealValidationException("Начальная дата не может быть позже конечной.");
    }

    // Обе границы периода включаются; агрегация выполняется на SQL Server.
    internal static IQueryable<DealSummary> SummaryQuery(
        EstateDbContext db, DateOnly from, DateOnly to) =>
        db.Deals.AsNoTracking()
            .Where(d => d.DealDate >= from && d.DealDate <= to)
            .GroupBy(_ => 1)
            .Select(g => new DealSummary
            {
                Count = g.Count(),
                TotalAmount = g.Sum(d => d.Amount),
                TotalCommission = g.Sum(d => d.CommissionAmount)
            });

    public async Task<DealSummary> GetSummaryAsync(DateOnly from, DateOnly to)
    {
        ValidatePeriod(from, to);
        await using var db = Db.Create();
        await WorkAccess.RequireAsync(db, WorkAccess.Reporting);
        return await SummaryQuery(db, from, to).SingleOrDefaultAsync() ?? new DealSummary();
    }
}
