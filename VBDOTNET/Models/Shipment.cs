using System;
using System.Collections.Generic;

namespace Wasl.Models;

public partial class Shipment
{
    public int ShipmentId { get; set; }

    public string? TrackingNumber { get; set; }

    public string? CurrentStatus { get; set; }

    public DateTime? ActualStartDate { get; set; }

    public DateTime? ActualDeliveryTime { get; set; }

    public int? ContractId { get; set; }

    public virtual Contract? Contract { get; set; }

    public virtual ICollection<Contract> Contracts { get; set; } = new List<Contract>();

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
}
