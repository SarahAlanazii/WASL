using System;
using System.Collections.Generic;

namespace Wasl.Models;

public partial class ShipmentRequest
{
    public int ShipmentRequestId { get; set; }

    public string? GoodsType { get; set; }

    public decimal? WeightKg { get; set; }

    public string? PickupLocation { get; set; }

    public string? PickupCity { get; set; }

    public string? PickupRegion { get; set; }

    public string? DeliveryLocation { get; set; }

    public string? DeliveryCity { get; set; }

    public string? DeliveryRegion { get; set; }

    public DateTime? DeliveryDeadline { get; set; }

    public string? Status { get; set; }

    public string? SpecialInstructions { get; set; }

    public DateTime? RequestDate { get; set; }

    public DateTime? UpdateAt { get; set; }

    public int? CompanyId { get; set; }

    public int? ProviderId { get; set; }

    public virtual ICollection<Bid> Bids { get; set; } = new List<Bid>();

    public virtual Company? Company { get; set; }

    public virtual Provider? Provider { get; set; }
}
