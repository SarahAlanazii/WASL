using Wasl.Models;

namespace Wasl.ViewModels.CompanyVMs
{
    public class TrackingDetailsViewModel
    {
        public List<Contract> Contracts { get; set; }
        public Contract SelectedContract { get; set; }
        public List<Shipment> Shipments { get; set; }
        public Shipment LatestShipment { get; set; }
        public bool CanProvideFeedback { get; set; }
        public bool HasProvidedFeedback { get; set; }
    }
}
