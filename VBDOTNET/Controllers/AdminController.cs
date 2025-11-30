using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wasl.Data;
using Wasl.Infrastructure;
using Wasl.ViewModels.AdminVMs;
using Wasl.ViewModels.Auth;

namespace Wasl.Controllers.Admin
{
    [Authorize(Policy = "AdminOnly")]
    public class AdminController : BaseController
    {
        public AdminController(WaslDbContext context, ILogger<AdminController> logger, IFileUploadService fileUpload)
            : base(context, logger, fileUpload) { }

        public async Task<IActionResult> Dashboard()
        {
            var stats = new AdminDashboardStats
            {
                TotalUsers = await _context.Users.Where(u => u.UserRole == AppConstants.ROLE_COMPANY || u.UserRole == AppConstants.ROLE_PROVIDER).CountAsync(),
                TotalCompanies = await _context.Companies.CountAsync(),
                TotalProviders = await _context.Providers.Where(p => p.IsApproved == true).CountAsync(),
                PendingProviders = await _context.Providers.Where(p => p.IsApproved == false).CountAsync(),
                TotalShipments = await _context.ShipmentRequests.CountAsync(),
                ActiveShipments = await _context.ShipmentRequests.Where(s => s.Status != AppConstants.SHIPMENT_DELIVERED && s.Status != AppConstants.SHIPMENT_CANCELLED).CountAsync(),
                TotalContracts = await _context.Contracts.CountAsync(),
                TotalRevenue = await _context.RevenueReports.SumAsync(r => r.TotalRevenue ?? 0),
                TotalFeedback = await _context.Feedbacks.CountAsync()
            };

            var recentShipments = await _context.ShipmentRequests.OrderByDescending(s => s.RequestDate).Take(5).ToListAsync();
            var recentContracts = await _context.Contracts.OrderByDescending(c => c.SignDate).Take(5).ToListAsync();
            var pendingApprovals = await _context.Providers.Where(p => p.IsApproved == false).Take(5).ToListAsync();

            return View(new AdminDashboardViewModel { Stats = stats, RecentShipments = recentShipments, RecentContracts = recentContracts, PendingApprovals = pendingApprovals });
        }

        public async Task<IActionResult> Companies()
        {
            //var companies = await _context.Companies.ToListAsync();
            var companies = await _context.Companies
                .Include(c => c.User) 
                .Include(c => c.ShipmentRequests) 
                .Include(c => c.Contracts) 
                .ToListAsync();

            var companiesWithCounts = new List<CompanyWithStats>();
            foreach (var company in companies)
            {
                companiesWithCounts.Add(new CompanyWithStats
                {
                    Company = company,
                    ShipmentCount = await _context.ShipmentRequests.Where(s => s.CompanyId == company.CompanyId).CountAsync(),
                    ContractCount = await _context.Contracts.Where(c => c.CompanyId == company.CompanyId).CountAsync()
                });
            }
            return View(companiesWithCounts);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleCompanyApproval(int id)
        {
            try
            {
                var company = await _context.Companies.FindAsync(id);
                if (company == null) return NotFound();
                company.IsApproved = !company.IsApproved;
                await _context.SaveChangesAsync();
                SetSuccessMessage($"Company {(company.IsApproved == true ? "approved" : "disapproved")} successfully!");
                return RedirectToAction(nameof(Companies));
            }
            catch (Exception ex)
            {
                LogAndSetError(ex, "Failed to update company status");
                return RedirectToAction(nameof(Companies));
            }
        }

        public async Task<IActionResult> Providers()
        {
            //var providers = await _context.Providers.ToListAsync();
            var providers = await _context.Providers
                .Include(p => p.User) 
                .Include(p => p.Bids) 
                .Include(p => p.Contracts) 
                .Include(p => p.Admin) 
                .ToListAsync();

            var providersWithStats = new List<ProviderWithStats>();
            foreach (var provider in providers)
            {
                providersWithStats.Add(new ProviderWithStats
                {
                    Provider = provider,
                    BidCount = await _context.Bids.Where(b => b.ProviderId == provider.ProviderId).CountAsync(),
                    ContractCount = await _context.Contracts.Where(c => c.ProviderId == provider.ProviderId).CountAsync()
                });
            }
            return View(providersWithStats);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleProviderApproval(int id)
        {
            try
            {
                var provider = await _context.Providers.FindAsync(id);
                if (provider == null) return NotFound();
                provider.IsApproved = !provider.IsApproved;
                await _context.SaveChangesAsync();
                SetSuccessMessage($"Provider {(provider.IsApproved == true ? "approved" : "disapproved")} successfully!");
                return RedirectToAction(nameof(Providers));
            }
            catch (Exception ex)
            {
                LogAndSetError(ex, "Failed to update provider status");
                return RedirectToAction(nameof(Providers));
            }
        }

        public async Task<IActionResult> ShipmentRequests()
        {
            //var shipments = await _context.ShipmentRequests.OrderByDescending(s => s.RequestDate).ToListAsync();

            var shipments = await _context.ShipmentRequests
                .Include(s => s.Company) 
                .Include(s => s.Bids) 
                    .ThenInclude(b => b.Provider)
                .OrderByDescending(s => s.RequestDate)
                .ToListAsync();

            var shipmentsWithBids = new List<ShipmentWithBids>();
            foreach (var shipment in shipments)
            {
                shipmentsWithBids.Add(new ShipmentWithBids
                {
                    Shipment = shipment,
                    BidCount = await _context.Bids.Where(b => b.ShipmentRequestId == shipment.ShipmentRequestId).CountAsync()
                });
            }
            return View(shipmentsWithBids);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteShipment(int id)
        {
            try
            {
                var shipment = await _context.ShipmentRequests.FindAsync(id);
                if (shipment == null) return NotFound();
                _context.ShipmentRequests.Remove(shipment);
                await _context.SaveChangesAsync();
                SetSuccessMessage("Shipment deleted successfully!");
                return RedirectToAction(nameof(ShipmentRequests));
            }
            catch (Exception ex)
            {
                LogAndSetError(ex, "Failed to delete shipment");
                return RedirectToAction(nameof(ShipmentRequests));
            }
        }
        public async Task<IActionResult> DeleteBid(int id)
        {
            try
            {
                var bid = await _context.Bids.FindAsync(id);
                if (bid == null) return NotFound();

                _context.Bids.Remove(bid);
                await _context.SaveChangesAsync();

                SetSuccessMessage("Bid deleted successfully!");
                return RedirectToAction(nameof(ShipmentRequests));
            }
            catch (Exception ex)
            {
                LogAndSetError(ex, "Failed to delete bid");
                return RedirectToAction(nameof(ShipmentRequests));
            }
        }

        //public async Task<IActionResult> Contracts()
        //{
        //    var contracts = await _context.Contracts.OrderByDescending(c => c.SignDate).ToListAsync();
        //    return View(contracts);
        //}
        public async Task<IActionResult> Contracts()
        {
            var contracts = await _context.Contracts
                .Include(c => c.Company)
                .Include(c => c.Provider)
                .Include(c => c.Bid)
                    .ThenInclude(b => b.ShipmentRequest)
                .Include(c => c.Shipments)
                .OrderByDescending(c => c.SignDate)
                .ToListAsync();

            return View(contracts);
        }

        //public async Task<IActionResult> Feedback()
        //{
        //    var feedbacks = await _context.Feedbacks.OrderByDescending(f => f.FeedbackDate).ToListAsync();
        //    return View(feedbacks);
        //}

        public async Task<IActionResult> Feedback()
        {
            var feedbacks = await _context.Feedbacks
                .Include(f => f.Company)
                .Include(f => f.Provider)
                .Include(f => f.Shipment) 
                    .ThenInclude(s => s.Contract) 
                        .ThenInclude(c => c.Bid) 
                            .ThenInclude(b => b.ShipmentRequest)
                .OrderByDescending(f => f.FeedbackDate)
                .ToListAsync();

            return View(feedbacks);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFeedback(int id)
        {
            try
            {
                var feedback = await _context.Feedbacks.FindAsync(id);
                if (feedback == null) return NotFound();
                _context.Feedbacks.Remove(feedback);
                await _context.SaveChangesAsync();
                SetSuccessMessage("Feedback deleted successfully!");
                return RedirectToAction(nameof(Feedback));
            }
            catch (Exception ex)
            {
                LogAndSetError(ex, "Failed to delete feedback");
                return RedirectToAction(nameof(Feedback));
            }
        }

        // Added -------------------------------------------------------------
        public async Task<IActionResult> Profile()
        {
            var userId = GetCurrentUserId();
            var admin = await _context.Admins
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.UserId == userId);
            return View(admin);
        }

        public async Task<IActionResult> EditProfile()
        {
            var userId = GetCurrentUserId();
            var admin = await _context.Admins.FirstOrDefaultAsync(a => a.UserId == userId);

            var model = new AdminProfileViewModel
            {
                AdminFirstName = admin.AdminFirstName,
                AdminLastName = admin.AdminLastName,
                AdminEmail = admin.AdminEmail,
                AdminPhoneNumber = admin.AdminPhoneNumber
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(AdminProfileViewModel model)
        {
            if (!ModelState.IsValid) return View("EditProfile", model);

            try
            {
                var userId = GetCurrentUserId();
                var admin = await _context.Admins.FirstOrDefaultAsync(a => a.UserId == userId);

                admin.AdminFirstName = model.AdminFirstName;
                admin.AdminLastName = model.AdminLastName;
                admin.AdminEmail = model.AdminEmail;
                admin.AdminPhoneNumber = model.AdminPhoneNumber;

                await _context.SaveChangesAsync();
                SetSuccessMessage("Profile updated successfully!");
                return RedirectToAction(nameof(Profile));
            }
            catch (Exception ex)
            {
                LogAndSetError(ex, "Failed to update profile");
                return View("EditProfile", model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                SetErrorMessage("Please provide valid password information");
                return RedirectToAction(nameof(EditProfile));
            }

            try
            {
                var userId = GetCurrentUserId();
                var user = await _context.Users.FindAsync(userId);

                if (!BCrypt.Net.BCrypt.Verify(model.CurrentPassword, user.UserPassword))
                {
                    SetErrorMessage("Current password is incorrect");
                    return RedirectToAction(nameof(EditProfile));
                }

                user.UserPassword = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
                await _context.SaveChangesAsync();

                SetSuccessMessage("Password updated successfully!");
                return RedirectToAction(nameof(EditProfile));
            }
            catch (Exception ex)
            {
                LogAndSetError(ex, "Failed to update password");
                return RedirectToAction(nameof(EditProfile));
            }
        }


    }
}
