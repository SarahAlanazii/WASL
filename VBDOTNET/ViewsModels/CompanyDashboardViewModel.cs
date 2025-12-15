using Wasl.Models;

namespace Wasl.ViewModels.CompanyVMs
{
    public class CompanyDashboardViewModel
    {
        public Company Company { get; set; }
        public CompanyDashboardStats Stats { get; set; }
        public MonthlySpendingData MonthlySpending { get; set; }
        public List<RecentActivityItem> RecentActivities { get; set; }
    }

        public class MonthlySpendingData
    {
        public string[] MonthLabels { get; set; }
        public List<decimal> SpendingData { get; set; }
    }

    public class RecentActivityItem
    {
        public string ActivityType { get; set; }
        public string ActivityTitle { get; set; }
        public string ActivityDescription { get; set; }
        public DateTime ActivityDate { get; set; }
        public string ActivityStatus { get; set; }
    }
}