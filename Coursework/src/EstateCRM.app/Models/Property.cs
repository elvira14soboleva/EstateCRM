using System;
using System.Collections.Generic;

namespace EstateCRM.App.Models;

public partial class Property
{
    public int PropertyId { get; set; }

    public int OwnerClientId { get; set; }

    public int PropertyTypeId { get; set; }

    public int DistrictId { get; set; }

    public int DealTypeId { get; set; }

    public int EmployeeId { get; set; }

    public int StatusId { get; set; }

    public string Address { get; set; } = null!;

    public decimal Area { get; set; }

    public byte Rooms { get; set; }

    public short? Floor { get; set; }

    public decimal Price { get; set; }

    public string? Description { get; set; }

    public string? PhotoPath { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual DealType DealType { get; set; } = null!;

    public virtual District District { get; set; } = null!;

    public virtual Employee Employee { get; set; } = null!;

    public virtual Client OwnerClient { get; set; } = null!;

    public virtual PropertyType PropertyType { get; set; } = null!;

    public virtual PropertyStatus Status { get; set; } = null!;

    public virtual ICollection<Viewing> Viewings { get; set; } = new List<Viewing>();
}
