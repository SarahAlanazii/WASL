using Wasl.Models;

namespace Wasl.ViewModels.CompanyVMs
{
    public class ProviderDetailsViewModel
    {
        public Provider Provider { get; set; }
        public double AverageRating { get; set; }
        public int RatingCount { get; set; }
        public List<Contract> CompletedShipments { get; set; }
        public List<ShipmentRequest> AvailableShipments { get; set; }
    }
}