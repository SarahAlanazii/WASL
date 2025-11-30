using System;
using System.Collections.Generic;

namespace Wasl.Models;

public partial class Provider
{
    public int ProviderId { get; set; }

    public int UserId { get; set; }

    public string? ProviderName { get; set; }

    public string? BusinessRegistrationNumber { get; set; }

    public string? ProviderAddress { get; set; }

    public string? ProviderCity { get; set; }

    public string? ProviderRegion { get; set; }

    public string? ServiceDescription { get; set; }

    public bool? IsApproved { get; set; }

    public string? ProviderEmail { get; set; }

    public string? ProviderPhoneNumber { get; set; }

    public int? AdminId { get; set; }

    public virtual Admin? Admin { get; set; }

    public virtual ICollection<Bid> Bids { get; set; } = new List<Bid>();

    public virtual ICollection<Contract> Contracts { get; set; } = new List<Contract>();

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

    public virtual ICollection<ShipmentRequest> ShipmentRequests { get; set; } = new List<ShipmentRequest>();

    public virtual User User { get; set; } = null!;
}
