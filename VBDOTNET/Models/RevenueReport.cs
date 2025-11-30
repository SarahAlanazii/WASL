using System;
using System.Collections.Generic;

namespace Wasl.Models;

public partial class RevenueReport
{
    public int ReportId { get; set; }

    public decimal? TotalCommission { get; set; }

    public DateTime? GenerateDate { get; set; }

    public decimal? TotalRevenue { get; set; }

    public string? PeriodCovered { get; set; }

    public int? AdminId { get; set; }

    public virtual Admin? Admin { get; set; }
}
