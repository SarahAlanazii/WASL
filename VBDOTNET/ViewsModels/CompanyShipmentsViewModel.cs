using Wasl.Models;

namespace Wasl.ViewModels.CompanyVMs
{
    public class CompanyShipmentsViewModel
    {
        public List<ShipmentRequest> Shipments { get; set; }
        public Dictionary<int, int> BidsCounts { get; set; }
    }
}
