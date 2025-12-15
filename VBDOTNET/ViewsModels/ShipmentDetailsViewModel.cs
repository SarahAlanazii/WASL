using Wasl.Models;

namespace Wasl.ViewModels.ShipmentVMs
{
    public class ShipmentDetailsViewModel
    {
        public ShipmentRequest Shipment { get; set; }
        public List<Bid> Bids { get; set; }
        public BidStatistics BidStats { get; set; }
    }
}
