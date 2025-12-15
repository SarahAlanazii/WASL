using Wasl.Models;

namespace Wasl.ViewModels.ProviderVMs
{
    public class ProviderContractsViewModel
    {
        public List<Contract> Contracts { get; set; }
        public string StatusFilter { get; set; } = "all";
        public int TotalItems { get; set; }
        public int PageSize { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
    }
}
