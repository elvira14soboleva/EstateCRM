using System;
using System.Collections.Generic;

namespace EstateCRM.App.Models;

public partial class Client
{
    public int ClientId { get; set; }

    public string LastName { get; set; } = null!;

    public string FirstName { get; set; } = null!;

    public string? MiddleName { get; set; }

    public string Phone { get; set; } = null!;

    public string? Email { get; set; }

    public string PassportSeries { get; set; } = null!;

    public string PassportNumber { get; set; } = null!;

    public DateOnly RegistrationDate { get; set; }

    public virtual ICollection<ClientRequest> ClientRequests { get; set; } = new List<ClientRequest>();

    public virtual ICollection<Property> Properties { get; set; } = new List<Property>();
}
