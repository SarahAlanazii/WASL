namespace Wasl.ViewModels.ShipmentVMs
{
    public class BidStatistics
    {
        public decimal AverageBid { get; set; }
        public decimal MinBid { get; set; }
        public decimal MaxBid { get; set; }
        public int ActiveBidders { get; set; }
    }
}
