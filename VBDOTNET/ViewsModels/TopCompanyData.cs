using Wasl.Models;

namespace Wasl.ViewModels.AdminVMs
{
    public class TopCompanyData
    {
        public Company Company { get; set; }
        public decimal Spending { get; set; }
        public int ContractsCount { get; set; }
    }
}
