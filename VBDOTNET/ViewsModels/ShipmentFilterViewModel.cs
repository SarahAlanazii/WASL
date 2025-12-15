namespace Wasl.ViewModels.ShipmentVMs
{
    public class ShipmentFilterViewModel
    {
        public string Search { get; set; }
        public string GoodsType { get; set; }
        public string Region { get; set; }
        public string City { get; set; }
        public string WeightRange { get; set; }
        public string Status { get; set; }
        public string Sort { get; set; } = "newest";
        public int? Page { get; set; } = 1;
    }
}
