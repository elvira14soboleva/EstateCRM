using System;
using System.Collections.Generic;

namespace EstateCRM.App.Models;

public partial class ClientRequest
{
    public int RequestId { get; set; }

    public int ClientId { get; set; }

    public int PropertyTypeId { get; set; }

    public int? DistrictId { get; set; }

    public int DealTypeId { get; set; }

    public decimal PriceMin { get; set; }

    public decimal PriceMax { get; set; }

    public decimal? AreaMin { get; set; }

    public byte? Rooms { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Client Client { get; set; } = null!;

    public virtual DealType DealType { get; set; } = null!;

    public virtual District? District { get; set; }

    public virtual PropertyType PropertyType { get; set; } = null!;

    public virtual ICollection<Viewing> Viewings { get; set; } = new List<Viewing>();
}
