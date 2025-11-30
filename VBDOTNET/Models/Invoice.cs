using System;
using System.Collections.Generic;

namespace Wasl.Models;

public partial class Invoice
{
    public int InvoiceId { get; set; }

    public string? InvoiceNumber { get; set; }

    public DateTime? InvoiceDate { get; set; }

    public DateTime? InvoiceDueDate { get; set; }

    public int? PaymentId { get; set; }

    public int? ContractId { get; set; }

    public virtual Contract? Contract { get; set; }

    public virtual Payment? Payment { get; set; }

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
