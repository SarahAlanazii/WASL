namespace Wasl.ViewModels.AdminVMs
{
    public class AdminDashboardStats
    {
        public int TotalUsers { get; set; }
        public int TotalCompanies { get; set; }
        public int TotalProviders { get; set; }
        public int PendingProviders { get; set; }
        public int TotalShipments { get; set; }
        public int ActiveShipments { get; set; }
        public int TotalContracts { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalFeedback { get; set; }
    }
}
