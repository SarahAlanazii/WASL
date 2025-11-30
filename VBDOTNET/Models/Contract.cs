using System;
using System.Collections.Generic;

namespace Wasl.Models;

public partial class Contract
{
    public int ContractId { get; set; }

    public string? ContractDocument { get; set; }

    public DateTime? SignDate { get; set; }

    public int? BidId { get; set; }

    public int? CompanyId { get; set; }

    public int? ProviderId { get; set; }

    public int? ShipmentId { get; set; }

    public virtual Bid? Bid { get; set; }

    public virtual Company? Company { get; set; }

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    public virtual Provider? Provider { get; set; }

    public virtual Shipment? Shipment { get; set; }

    public virtual ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();
}
