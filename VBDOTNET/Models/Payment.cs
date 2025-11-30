using System;
using System.Collections.Generic;

namespace Wasl.Models;

public partial class Payment
{
    public int PaymentId { get; set; }

    public decimal? PaymentAmount { get; set; }

    public string? PaymentMethod { get; set; }

    public string? PaymentStatus { get; set; }

    public string? TransactionId { get; set; }

    public DateTime? PaymentDate { get; set; }

    public int? InvoiceId { get; set; }

    public virtual Invoice? Invoice { get; set; }

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}
