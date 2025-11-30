using System;
using System.Collections.Generic;

namespace Wasl.Models;

public partial class Feedback
{
    public int FeedbackId { get; set; }

    public int? Rating { get; set; }

    public string? Comments { get; set; }

    public DateTime? FeedbackDate { get; set; }

    public int? ProviderId { get; set; }

    public int? ShipmentId { get; set; }

    public int? CompanyId { get; set; }

    public virtual Company? Company { get; set; }

    public virtual Provider? Provider { get; set; }

    public virtual Shipment? Shipment { get; set; }
}
