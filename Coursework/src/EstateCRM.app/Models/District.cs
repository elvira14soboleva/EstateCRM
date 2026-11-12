using System;
using System.Collections.Generic;

namespace EstateCRM.App.Models;

public partial class District
{
    public int DistrictId { get; set; }

    public string DistrictName { get; set; } = null!;

    public virtual ICollection<ClientRequest> ClientRequests { get; set; } = new List<ClientRequest>();

    public virtual ICollection<Property> Properties { get; set; } = new List<Property>();
}
