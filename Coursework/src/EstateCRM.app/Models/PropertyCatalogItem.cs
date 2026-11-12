using System;
using System.Collections.Generic;
using System.Globalization;

namespace EstateCRM.App.Models;

public sealed class PropertyCatalogItem
{
    private static readonly CultureInfo Ru = CultureInfo.GetCultureInfo("ru-RU");
    public int PropertyId { get; init; }
    public string Address { get; init; } = "";
    public string TypeName { get; init; } = "";
    public string DistrictName { get; init; } = "";
    public string DealTypeName { get; init; } = "";
    public string StatusName { get; init; } = "";
    public decimal Area { get; init; }
    public byte Rooms { get; init; }
    public short? Floor { get; init; }
    public decimal Price { get; init; }
    public string? PhotoPath { get; init; }
    public string? Description { get; init; }
    public DateTime CreatedAt { get; init; }
    public string NumberText => $"ОБЪЕКТ № {PropertyId:000}";
    public string CategoryText => $"{TypeName} · {DealTypeName}";
    public string PriceText => Price.ToString("N2", Ru) + " ₽";
    public string FactsText => Area.ToString("0.##", Ru) + " м² · "
        + (Rooms == 0 ? "Без деления на комнаты" : $"Комнат: {Rooms}");
    public string FloorText => Floor?.ToString(Ru) ?? "Не указан";
    public string DateText => CreatedAt.ToString("dd.MM.yyyy", Ru);
    public string DescriptionText => string.IsNullOrWhiteSpace(Description)
        ? "Описание не добавлено." : Description;
}

public sealed record PropertyCatalogOption(int Id, string Name)
{
    public override string ToString() => Name;
}

public sealed class PropertyCatalogFilters
{
    public IReadOnlyList<PropertyCatalogOption> Types { get; init; } = Array.Empty<PropertyCatalogOption>();
    public IReadOnlyList<PropertyCatalogOption> Districts { get; init; } = Array.Empty<PropertyCatalogOption>();
    public IReadOnlyList<PropertyCatalogOption> DealTypes { get; init; } = Array.Empty<PropertyCatalogOption>();
    public IReadOnlyList<PropertyCatalogOption> Statuses { get; init; } = Array.Empty<PropertyCatalogOption>();
}

public sealed class PropertyCatalogQuery
{
    public string Address { get; init; } = "";
    public int? TypeId { get; init; }
    public int? DistrictId { get; init; }
    public int? DealTypeId { get; init; }
    public int? StatusId { get; init; }
    public decimal? PriceMin { get; init; }
    public decimal? PriceMax { get; init; }
    public int Sort { get; init; }
}
