using System;
using System.Collections.Generic;

namespace EstateCRM.App.Models;

public partial class Deal
{
    public int DealId { get; set; }

    public int ViewingId { get; set; }

    public int EmployeeId { get; set; }

    public DateOnly DealDate { get; set; }

    public string ContractNumber { get; set; } = null!;

    public string ContractFile { get; set; } = null!;

    public decimal Amount { get; set; }

    public decimal CommissionPercent { get; set; }

    public decimal CommissionAmount { get; set; }

    public virtual Employee Employee { get; set; } = null!;

    public virtual Viewing Viewing { get; set; } = null!;
}
