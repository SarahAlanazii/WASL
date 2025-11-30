using System;
using System.Collections.Generic;

namespace Wasl.Models;

public partial class Admin
{
    public int AdminId { get; set; }

    public int UserId { get; set; }

    public string? AdminEmail { get; set; }

    public string? AdminPhoneNumber { get; set; }

    public string? AdminStatus { get; set; }

    public string? AdminFirstName { get; set; }

    public string? AdminLastName { get; set; }

    public string? AdminRole { get; set; }

    public virtual ICollection<Company> Companies { get; set; } = new List<Company>();

    public virtual ICollection<Provider> Providers { get; set; } = new List<Provider>();

    public virtual ICollection<RevenueReport> RevenueReports { get; set; } = new List<RevenueReport>();

    public virtual User User { get; set; } = null!;
}
