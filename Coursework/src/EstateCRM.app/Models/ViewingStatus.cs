using System;
using System.Collections.Generic;

namespace EstateCRM.App.Models;

public partial class ViewingStatus
{
    public int ViewingStatusId { get; set; }

    public string StatusName { get; set; } = null!;

    public virtual ICollection<Viewing> Viewings { get; set; } = new List<Viewing>();
}
