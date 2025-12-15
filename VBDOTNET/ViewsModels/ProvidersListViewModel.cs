using Wasl.Models;

namespace Wasl.ViewModels.CompanyVMs
{
    public class ProvidersListViewModel
    {
        public List<Provider> Providers { get; set; }
        public int TotalItems { get; set; }
        public int PageSize { get; set; } = 12;
        public int CurrentPage { get; set; } = 1;
        public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
    }
}
