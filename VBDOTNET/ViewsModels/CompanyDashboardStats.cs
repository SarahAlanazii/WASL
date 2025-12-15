namespace Wasl.ViewModels.CompanyVMs
{
    public class CompanyDashboardStats
    {
        public int ActiveShipments { get; set; }
        public int PendingRequests { get; set; }
        public int TotalContracts { get; set; }
        public int PendingBids { get; set; }
        public int CompletedShipments { get; set; }
        public decimal TotalSpent { get; set; }
    }
}
