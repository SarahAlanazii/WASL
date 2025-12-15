using Wasl.Models;

namespace Wasl.ViewModels.AdminVMs
{
    public class ProviderWithStats
    {
        public Provider Provider { get; set; }
        public int BidCount { get; set; }
        public int ContractCount { get; set; }
    }
}
