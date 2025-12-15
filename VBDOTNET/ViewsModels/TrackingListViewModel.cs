using Wasl.Models;

namespace Wasl.ViewModels.CompanyVMs
{
    public class TrackingListViewModel
    {
        public List<Shipment> Shipments { get; set; }
        public List<string> Statuses { get; set; }
        public int TotalItems { get; set; }
    }
}
