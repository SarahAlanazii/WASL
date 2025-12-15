using Wasl.Models;

namespace Wasl.ViewModels.ShipmentVMs
{
    public class ShipmentsListViewModel
    {
        public List<ShipmentRequest> Shipments { get; set; }
        public Dictionary<int, int> BidsCounts { get; set; }
        public List<string> GoodsTypes { get; set; }
        public Dictionary<string, string> Regions { get; set; }
        public ShipmentFilterViewModel Filter { get; set; }
        public int TotalItems { get; set; }
        public int PageSize { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
    }
}
