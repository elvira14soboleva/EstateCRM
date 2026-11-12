using System;
using System.Collections.Generic;

namespace EstateCRM.App.Models;

public partial class Viewing
{
    public int ViewingId { get; set; }

    public int ViewingStatusId { get; set; }

    public int PropertyId { get; set; }

    public int RequestId { get; set; }

    public DateTime ViewingDatetime { get; set; }

    public string? ResultComment { get; set; }

    public virtual Deal? Deal { get; set; }

    public virtual Property Property { get; set; } = null!;

    public virtual ClientRequest Request { get; set; } = null!;

    public virtual ViewingStatus ViewingStatus { get; set; } = null!;
}
