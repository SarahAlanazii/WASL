using Wasl.Models;

namespace Wasl.ViewModels.CompanyVMs
{
    public class CompanyBidsViewModel
    {
        public List<Bid> Bids { get; set; }
        public List<ShipmentRequest> Shipments { get; set; }
    }
}
