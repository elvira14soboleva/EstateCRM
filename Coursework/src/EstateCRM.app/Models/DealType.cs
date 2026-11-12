using System;
using System.Collections.Generic;

namespace EstateCRM.App.Models;

public partial class DealType
{
    public int DealTypeId { get; set; }

    public string DealTypeName { get; set; } = null!;

    public decimal CommissionPercent { get; set; }

    public virtual ICollection<ClientRequest> ClientRequests { get; set; } = new List<ClientRequest>();

    public virtual ICollection<Property> Properties { get; set; } = new List<Property>();
}
