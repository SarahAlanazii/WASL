using Wasl.Models;

namespace Wasl.ViewModels.AdminVMs
{
    public class AdminDashboardViewModel
    {
        public AdminDashboardStats Stats { get; set; }
        public List<ShipmentRequest> RecentShipments { get; set; }
        public List<Contract> RecentContracts { get; set; }
        public List<Provider> PendingApprovals { get; set; }
    }
}
