namespace Wasl.ViewModels.AdminVMs
{
    public class RevenueReportViewModel
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public RevenueData RevenueData { get; set; }
        public List<TopCompanyData> TopCompanies { get; set; }
        public List<TopProviderData> TopProviders { get; set; }
    }
}
