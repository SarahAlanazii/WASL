using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wasl.Data;
using Wasl.Infrastructure;
using Wasl.Models;
using Wasl.ViewModels.AdminVMs;

namespace Wasl.Controllers.Admin
{
    [Authorize(Policy = "AdminOnly")]
    public class ReportController : BaseController
    {
        public ReportController(WaslDbContext context, ILogger<ReportController> logger, IFileUploadService fileUpload)
            : base(context, logger, fileUpload) { }

        public async Task<IActionResult> Revenue(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var start = startDate ?? DateTime.Now.AddMonths(-1);
                var end = endDate ?? DateTime.Now;

                var revenueData = await CalculateRevenueData(start, end);

                var viewModel = new RevenueReportViewModel
                {
                    StartDate = start,
                    EndDate = end,
                    RevenueData = revenueData,
                    TopCompanies = await GetTopCompanies(start, end),
                    TopProviders = await GetTopProviders(start, end)
                };

                //return View(viewModel);
                return View("~/Views/Admin/Reports/Revenue.cshtml", viewModel);
            }
            catch (Exception ex)
            {
                LogAndSetError(ex, "Error generating revenue report");
                return RedirectToAction("Dashboard", "Admin");
            }
        }

        private async Task<RevenueData> CalculateRevenueData(DateTime start, DateTime end)
        {
            // CRITICAL: Include Bid navigation property to avoid null reference
            var contracts = await _context.Contracts
                .Include(c => c.Bid)
                .Include(c => c.Invoices)
                    .ThenInclude(i => i.Payment)
                .Where(c => c.SignDate >= start && c.SignDate <= end)
                .ToListAsync();

            var totalRevenue = contracts.Sum(c => c.Bid?.BidPrice ?? 0);
            var commissionRate = 0.10m;

            // Calculate payment status
            var paymentStatus = new Dictionary<string, int>
            {
                { "Completed", 0 },
                { "Pending", 0 },
                { "Failed", 0 }
            };

            foreach (var contract in contracts)
            {
                var payment = contract.Invoices?.FirstOrDefault()?.Payment;
                if (payment != null)
                {
                    var status = payment.PaymentStatus switch
                    {
                        AppConstants.PAYMENT_SUCCESSFUL => "Completed",
                        AppConstants.PAYMENT_FAILED or AppConstants.PAYMENT_REFUNDED => "Failed",
                        _ => "Pending"
                    };
                    paymentStatus[status]++;
                }
                else
                {
                    paymentStatus["Pending"]++;
                }
            }

            // Calculate monthly revenue
            var monthlyRevenue = CalculateMonthlyRevenue(contracts, start, end, commissionRate);

            return new RevenueData
            {
                TotalRevenue = totalRevenue,
                TotalCommission = totalRevenue * commissionRate,
                CommissionRate = commissionRate * 100,
                TotalContracts = contracts.Count,
                PaymentStatus = paymentStatus,
                MonthlyRevenue = monthlyRevenue,
                Contracts = contracts
            };
        }

        private List<MonthlyRevenueData> CalculateMonthlyRevenue(
            List<Contract> contracts,
            DateTime start,
            DateTime end,
            decimal commissionRate)
        {
            var monthlyRevenue = new List<MonthlyRevenueData>();
            var current = new DateTime(start.Year, start.Month, 1);
            var endMonth = new DateTime(end.Year, end.Month, 1);

            while (current <= endMonth)
            {
                var nextMonth = current.AddMonths(1);
                var monthContracts = contracts
                    .Where(c => c.SignDate >= current && c.SignDate < nextMonth)
                    .ToList();

                var monthRevenue = monthContracts.Sum(c => c.Bid?.BidPrice ?? 0);

                monthlyRevenue.Add(new MonthlyRevenueData
                {
                    Month = current.ToString("MMM yyyy"),
                    Revenue = monthRevenue,
                    Commission = monthRevenue * commissionRate
                });

                current = nextMonth;
            }

            return monthlyRevenue;
        }

        private async Task<List<TopCompanyData>> GetTopCompanies(DateTime start, DateTime end)
        {
            // OPTIMIZED: Single query instead of N+1 queries
            return await _context.Companies
                .Where(c => c.IsApproved == true)
                .Select(c => new TopCompanyData
                {
                    Company = c,
                    Spending = c.Contracts
                        .Where(ct => ct.SignDate >= start && ct.SignDate <= end && ct.Bid != null)
                        .Sum(ct => ct.Bid.BidPrice ?? 0),
                    ContractsCount = c.Contracts
                        .Count(ct => ct.SignDate >= start && ct.SignDate <= end)
                })
                .Where(c => c.Spending > 0)
                .OrderByDescending(c => c.Spending)
                .Take(10)
                .ToListAsync();
        }

        private async Task<List<TopProviderData>> GetTopProviders(DateTime start, DateTime end)
        {
            // OPTIMIZED: Single query instead of N+1 queries
            return await _context.Providers
                .Where(p => p.IsApproved == true)
                .Select(p => new TopProviderData
                {
                    Provider = p,
                    Earnings = p.Contracts
                        .Where(c => c.SignDate >= start && c.SignDate <= end && c.Bid != null)
                        .Sum(c => c.Bid.BidPrice ?? 0),
                    ContractsCount = p.Contracts
                        .Count(c => c.SignDate >= start && c.SignDate <= end)
                })
                .Where(p => p.Earnings > 0)
                .OrderByDescending(p => p.Earnings)
                .Take(10)
                .ToListAsync();
        }
    }
}