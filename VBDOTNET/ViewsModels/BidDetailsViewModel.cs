using Wasl.Models;

namespace Wasl.ViewModels.BidVMs
{
    public class BidDetailsViewModel
    {
        public Bid Bid { get; set; }
        public double ProviderRating { get; set; }
        public int TotalFeedbacks { get; set; }
    }

}
