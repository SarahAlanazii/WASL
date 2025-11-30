using System;
using System.Collections.Generic;

namespace Wasl.Models;

public partial class Bid
{
    public int BidId { get; set; }

    public decimal? BidPrice { get; set; }

    public int? EstimatedDeliveryDays { get; set; }

    public string? BidNotes { get; set; }

    public string? BidStatus { get; set; }

    public DateTime? SubmitDate { get; set; }

    public int? ShipmentRequestId { get; set; }

    public int? ProviderId { get; set; }

    public virtual ICollection<Contract> Contracts { get; set; } = new List<Contract>();

    public virtual Provider? Provider { get; set; }

    public virtual ShipmentRequest? ShipmentRequest { get; set; }
}
