using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wasl.Data;
using Wasl.Infrastructure;
using Wasl.ViewModels.CompanyVMs;

namespace Wasl.Controllers.Company
{
    [Authorize(Policy = "CompanyOnly")]
    public class CompanyController : BaseController
    {
        public CompanyController(WaslDbContext context, ILogger<CompanyController> logger, IFileUploadService fileUpload)
            : base(context, logger, fileUpload) { }

        public async Task<IActionResult> Dashboard()
        {
            var userId = GetCurrentUserId();
            var company = await _context.Companies.FirstOrDefaultAsync(c => c.UserId == userId);
            if (company == null)
            {
                SetErrorMessage("Company profile not found");
                return RedirectToAction("Index", "Front");
            }

            var stats = new CompanyDashboardStats
            {
                ActiveShipments = await _context.ShipmentRequests
                    .CountAsync(s => s.CompanyId == company.CompanyId &&
                                   s.Status == AppConstants.SHIPMENT_IN_PROGRESS),
                PendingRequests = await _context.ShipmentRequests
                    .CountAsync(s => s.CompanyId == company.CompanyId &&
                                   s.Status == AppConstants.SHIPMENT_PENDING),
                TotalContracts = await _context.Contracts
                    .CountAsync(c => c.CompanyId == company.CompanyId),
                PendingBids = await _context.Bids
                    .CountAsync(b => b.ShipmentRequest.CompanyId == company.CompanyId &&
                                   b.BidStatus == AppConstants.BID_SUBMITTED),
                CompletedShipments = await _context.ShipmentRequests
                    .CountAsync(s => s.CompanyId == company.CompanyId &&
                                   s.Status == AppConstants.SHIPMENT_DELIVERED)
            };

            var contracts = await _context.Contracts
                .Where(c => c.CompanyId == company.CompanyId)
                .Include(c => c.Bid)
                .ToListAsync();

            stats.TotalSpent = contracts.Sum(c => c.Bid?.BidPrice ?? 0);

            var monthlySpending = GetMonthlySpendingData(company);
            var recentActivities = await GetRecentActivities(company);

            var viewModel = new CompanyDashboardViewModel
            {
                Company = company,
                Stats = stats,
                MonthlySpending = monthlySpending,
                RecentActivities = recentActivities
            };

            return View(viewModel);
        }


        /// <summary>
        /// NEW: Get monthly spending data for charts
        /// </summary>
        private MonthlySpendingData GetMonthlySpendingData(Models.Company company)
        {
            var currentYear = DateTime.Now.Year;
            var monthlyData = new List<decimal>();

            for (int month = 1; month <= 12; month++)
            {
                var total = _context.Contracts
                    .Where(c => c.CompanyId == company.CompanyId &&
                               c.SignDate.HasValue &&
                               c.SignDate.Value.Year == currentYear &&
                               c.SignDate.Value.Month == month)
                    .Include(c => c.Bid)
                    .ToList()
                    .Sum(c => c.Bid?.BidPrice ?? 0);

                monthlyData.Add(total);
            }

            return new MonthlySpendingData
            {
                MonthLabels = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun",
                             "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" },
                SpendingData = monthlyData
            };
        }

        /// <summary>
        /// NEW: Get recent activities for dashboard
        /// </summary>
        private async Task<List<RecentActivityItem>> GetRecentActivities(Models.Company company)
        {
            var activities = new List<RecentActivityItem>();

            // Recent shipments
            var recentShipments = await _context.ShipmentRequests
                .Where(s => s.CompanyId == company.CompanyId)
                .OrderByDescending(s => s.RequestDate)
                .Take(3)
                .ToListAsync();

            activities.AddRange(recentShipments.Select(s => new RecentActivityItem
            {
                ActivityType = "shipment",
                ActivityTitle = "New Shipment Request",
                ActivityDescription = $"{s.GoodsType} to {s.DeliveryCity}",
                ActivityDate = s.RequestDate ?? DateTime.Now,
                ActivityStatus = s.Status
            }));

            // Recent contracts
            var recentContracts = await _context.Contracts
                .Where(c => c.CompanyId == company.CompanyId)
                .Include(c => c.Provider)
                .Include(c => c.Bid)
                .OrderByDescending(c => c.SignDate)
                .Take(3)
                .ToListAsync();

            activities.AddRange(recentContracts.Select(c => new RecentActivityItem
            {
                ActivityType = "contract",
                ActivityTitle = "Contract Signed",
                ActivityDescription = $"With {c.Provider?.ProviderName}",
                ActivityDate = c.SignDate ?? DateTime.Now,
                ActivityStatus = "signed"
            }));

            return activities.OrderByDescending(a => a.ActivityDate).Take(5).ToList();
        }

        public async Task<IActionResult> Profile()
        {
            var userId = GetCurrentUserId();
            var company = await _context.Companies.FirstOrDefaultAsync(c => c.UserId == userId);

            if (company == null)
                return NotFound();

            var user = await _context.Users.FindAsync(userId);

            var model = new CompanyProfileViewModel
            {
                CompanyName = company.CompanyName,
                UserEmail = user?.UserEmail,
                BusinessRegistrationNumber = company.BusinessRegistrationNumber,
                PhoneNumber = company.PhoneNumber,
                CompanyRegion = company.CompanyRegion,
                CompanyCity = company.CompanyCity,
                CompanyAddress = company.CompanyAddress
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(CompanyProfileViewModel model)
        {
            if (!ModelState.IsValid)
                return View("Profile", model);

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var userId = GetCurrentUserId();
                var user = await _context.Users.FindAsync(userId);
                var company = await _context.Companies.FirstOrDefaultAsync(c => c.UserId == userId);

                if (user == null || company == null) return NotFound();

                user.UserName = model.CompanyName;
                user.UserEmail = model.UserEmail;
                user.UpdatedAt = DateTime.Now;

                company.CompanyName = model.CompanyName;
                company.BusinessRegistrationNumber = model.BusinessRegistrationNumber;
                company.PhoneNumber = model.PhoneNumber;
                company.CompanyRegion = model.CompanyRegion;
                company.CompanyCity = model.CompanyCity;
                company.CompanyAddress = model.CompanyAddress;
                company.CompanyEmail = model.UserEmail;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                SetSuccessMessage("Profile updated successfully!");
                return RedirectToAction(nameof(Profile));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                LogAndSetError(ex, "Failed to update profile");
                return View("Profile", model);
            }
        }

        /// <summary>
        /// NEW: Delete company account
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccount(DeleteAccountViewModel model)
        {
            if (!ModelState.IsValid)
                return View("Profile", new CompanyProfileViewModel());

            var userId = GetCurrentUserId();
            var user = await _context.Users.FindAsync(userId);
            var company = await _context.Companies.FirstOrDefaultAsync(c => c.UserId == userId);

            if (user == null || company == null)
                return NotFound();

            // Validate password
            if (!VerifyPassword(model.Password, user.UserPassword))
            {
                ModelState.AddModelError("Password", "Invalid password. Account deletion failed.");
                return View("Profile", new CompanyProfileViewModel
                {
                    CompanyName = company.CompanyName,
                    UserEmail = user.UserEmail,
                    BusinessRegistrationNumber = company.BusinessRegistrationNumber,
                    PhoneNumber = company.PhoneNumber,
                    CompanyRegion = company.CompanyRegion,
                    CompanyCity = company.CompanyCity,
                    CompanyAddress = company.CompanyAddress
                });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Delete company and user
                _context.Companies.Remove(company);
                _context.Users.Remove(user);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                SetSuccessMessage("Your account has been deleted successfully.");
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                LogAndSetError(ex, "Failed to delete account. Please try again.");
                return View("Profile", new CompanyProfileViewModel
                {
                    CompanyName = company.CompanyName,
                    UserEmail = user.UserEmail,
                    BusinessRegistrationNumber = company.BusinessRegistrationNumber,
                    PhoneNumber = company.PhoneNumber,
                    CompanyRegion = company.CompanyRegion,
                    CompanyCity = company.CompanyCity,
                    CompanyAddress = company.CompanyAddress
                });
            }
        }

        /// <summary>
        /// NEW: Password verification helper
        /// </summary>
        private bool VerifyPassword(string inputPassword, string storedHash)
        {
            // Implement your password verification logic here
            return true; // Placeholder for testing
        }
    }
}