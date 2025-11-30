using System;
using System.Collections.Generic;

namespace Wasl.Models;

public partial class Company
{
    public int CompanyId { get; set; }

    public int UserId { get; set; }

    public string? CompanyName { get; set; }

    public string? BusinessRegistrationNumber { get; set; }

    public string? CompanyAddress { get; set; }

    public string? CompanyCity { get; set; }

    public string? CompanyRegion { get; set; }

    public string? CompanyEmail { get; set; }

    public string? PhoneNumber { get; set; }

    public bool? IsApproved { get; set; }

    public string? CompanyStatus { get; set; }

    public int? AdminId { get; set; }

    public virtual Admin? Admin { get; set; }

    public virtual ICollection<Contract> Contracts { get; set; } = new List<Contract>();

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

    public virtual ICollection<ShipmentRequest> ShipmentRequests { get; set; } = new List<ShipmentRequest>();

    public virtual User User { get; set; } = null!;
}
