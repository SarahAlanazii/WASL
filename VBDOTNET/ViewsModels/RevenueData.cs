using Wasl.Models;

namespace Wasl.ViewModels.AdminVMs
{
    public class RevenueData
    {
        public decimal TotalRevenue { get; set; }
        public decimal TotalCommission { get; set; }
        public decimal CommissionRate { get; set; }
        public int TotalContracts { get; set; }
        public List<Contract> Contracts { get; set; }

        public Dictionary<string, int> PaymentStatus { get; set; } = new Dictionary<string, int>();
        public List<MonthlyRevenueData> MonthlyRevenue { get; set; } = new List<MonthlyRevenueData>();
    }
}