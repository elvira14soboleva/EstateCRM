using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EstateCRM.App.Data;
using EstateCRM.App.Models;
using Microsoft.EntityFrameworkCore;

namespace EstateCRM.App.Services;

// Только чтение. Не создаёт таблицы и не изменяет существующие записи.
public sealed class PropertyService
{
    public async Task<PropertyCatalogFilters> GetFiltersAsync(
        CancellationToken token = default)
    {
        await using var db = Db.Create();
        var types = await db.Set<PropertyType>().AsNoTracking()
            .OrderBy(x => x.TypeName)
            .Select(x => new PropertyCatalogOption(x.PropertyTypeId, x.TypeName))
            .ToListAsync(token);
        var districts = await db.Set<District>().AsNoTracking()
            .OrderBy(x => x.DistrictName)
            .Select(x => new PropertyCatalogOption(x.DistrictId, x.DistrictName))
            .ToListAsync(token);
        var dealTypes = await db.Set<DealType>().AsNoTracking()
            .OrderBy(x => x.DealTypeName)
            .Select(x => new PropertyCatalogOption(x.DealTypeId, x.DealTypeName))
            .ToListAsync(token);
        var statuses = await db.Set<PropertyStatus>().AsNoTracking()
            .OrderBy(x => x.StatusName)
            .Select(x => new PropertyCatalogOption(x.StatusId, x.StatusName))
            .ToListAsync(token);
        statuses.Insert(0, new PropertyCatalogOption(0, "Все статусы"));
        types.Insert(0, new PropertyCatalogOption(0, "Все типы"));
        districts.Insert(0, new PropertyCatalogOption(0, "Все районы"));
        dealTypes.Insert(0, new PropertyCatalogOption(0, "Все виды сделок"));
        return new PropertyCatalogFilters
        {
            Types = types, Districts = districts, DealTypes = dealTypes, Statuses = statuses
        };
    }

    public async Task<IReadOnlyList<PropertyCatalogItem>> GetCatalogAsync(
        PropertyCatalogQuery filter, CancellationToken token = default)
    {
        await using var db = Db.Create();
        var query = db.Set<Property>().AsNoTracking();
        string address = filter.Address.Trim();
        if (address.Length > 0)
            query = query.Where(x => x.Address.Contains(address));
        if (filter.TypeId is int type)
            query = query.Where(x => x.PropertyTypeId == type);
        if (filter.DistrictId is int district)
            query = query.Where(x => x.DistrictId == district);
        if (filter.DealTypeId is int deal)
            query = query.Where(x => x.DealTypeId == deal);
        if (filter.StatusId is int status)
            query = query.Where(x => x.StatusId == status);
        if (filter.PriceMin is decimal min)
            query = query.Where(x => x.Price >= min);
        if (filter.PriceMax is decimal max)
            query = query.Where(x => x.Price <= max);

        var ordered = filter.Sort switch
        {
            1 => query.OrderBy(x => x.Price).ThenBy(x => x.PropertyId),
            2 => query.OrderByDescending(x => x.Price).ThenBy(x => x.PropertyId),
            3 => query.OrderBy(x => x.Area).ThenBy(x => x.PropertyId),
            4 => query.OrderByDescending(x => x.Area).ThenBy(x => x.PropertyId),
            5 => query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.PropertyId),
            _ => query.OrderBy(x => x.PropertyId)
        };
        return await ordered.Select(x => new PropertyCatalogItem
        {
            PropertyId = x.PropertyId, Address = x.Address,
            TypeName = x.PropertyType.TypeName,
            DistrictName = x.District.DistrictName,
            DealTypeName = x.DealType.DealTypeName,
            StatusName = x.Status.StatusName,
            Area = x.Area, Rooms = x.Rooms, Floor = x.Floor,
            Price = x.Price, PhotoPath = x.PhotoPath,
            Description = x.Description, CreatedAt = x.CreatedAt
        }).ToListAsync(token);
    }
}
