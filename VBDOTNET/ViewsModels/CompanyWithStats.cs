using Wasl.Models;

namespace Wasl.ViewModels.AdminVMs
{
    public class CompanyWithStats
    {
        public Company Company { get; set; }
        public int ShipmentCount { get; set; }
        public int ContractCount { get; set; }
    }
}
