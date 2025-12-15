using Wasl.Models;

namespace Wasl.ViewModels.AdminVMs
{
    public class ShipmentWithBids
    {
        public ShipmentRequest Shipment { get; set; }
        public int BidCount { get; set; }
    }
}
