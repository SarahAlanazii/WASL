using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wasl.Data;
using Wasl.Infrastructure;
using Wasl.ViewModels;
using Wasl.ViewModels.Auth;
using Wasl.ViewModels.ProviderVMS;

namespace Wasl.Controllers
{
    [Authorize(Policy = "ProviderOnly")]
    public class ProviderProfileController : BaseController
    {
        public ProviderProfileController(WaslDbContext context, ILogger<ProviderProfileController> logger, IFileUploadService fileUpload)
            : base(context, logger, fileUpload) { }

        public async Task<IActionResult> Show()
        {
            var userId = GetCurrentUserId();
            var provider = await _context.Providers.FirstOrDefaultAsync(p => p.UserId == userId);
            if (provider == null) return NotFound();

            var recentBids = await _context.Bids.Where(b => b.ProviderId == provider.ProviderId).OrderByDescending(b => b.SubmitDate).Take(5).ToListAsync();
            var recentContracts = await _context.Contracts.Where(c => c.ProviderId == provider.ProviderId).OrderByDescending(c => c.SignDate).Take(5).ToListAsync();

            ViewBag.RecentBids = recentBids;
            ViewBag.RecentContracts = recentContracts;

            return View(provider);
        }

        public async Task<IActionResult> Edit()
        {
            var userId = GetCurrentUserId();
            var provider = await _context.Providers.FirstOrDefaultAsync(p => p.UserId == userId);
            if (provider == null) return NotFound();

            ViewBag.Regions = KSALocations.Regions;
            ViewBag.Cities = KSALocations.CitiesByRegion.ContainsKey(provider.ProviderRegion) ? KSALocations.CitiesByRegion[provider.ProviderRegion] : new Dictionary<string, string>();

            var model = new ProviderProfileViewModel
            {
                ProviderName = provider.ProviderName,
                BusinessRegistrationNumber = provider.BusinessRegistrationNumber,
                ProviderAddress = provider.ProviderAddress,
                ProviderRegion = provider.ProviderRegion,
                ProviderCity = provider.ProviderCity,
                ServiceDescription = provider.ServiceDescription,
                ProviderEmail = provider.ProviderEmail,
                ProviderPhoneNumber = provider.ProviderPhoneNumber
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(ProviderProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Regions = KSALocations.Regions;
                return View("Edit", model);
            }

            try
            {
                var userId = GetCurrentUserId();
                var provider = await _context.Providers.FirstOrDefaultAsync(p => p.UserId == userId);
                if (provider == null) return NotFound();

                provider.ProviderName = model.ProviderName;
                provider.BusinessRegistrationNumber = model.BusinessRegistrationNumber;
                provider.ProviderAddress = model.ProviderAddress;
                provider.ProviderRegion = model.ProviderRegion;
                provider.ProviderCity = model.ProviderCity;
                provider.ServiceDescription = model.ServiceDescription;
                provider.ProviderEmail = model.ProviderEmail;
                provider.ProviderPhoneNumber = model.ProviderPhoneNumber;

                await _context.SaveChangesAsync();

                SetSuccessMessage("Profile updated successfully!");
                return RedirectToAction(nameof(Show));
            }
            catch (Exception ex)
            {
                LogAndSetError(ex, "Failed to update profile");
                ViewBag.Regions = KSALocations.Regions;
                return View("Edit", model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                SetErrorMessage("Please provide valid password information");
                return RedirectToAction(nameof(Show));
            }

            try
            {
                var userId = GetCurrentUserId();
                var user = await _context.Users.FindAsync(userId);

                if (user == null || !BCrypt.Net.BCrypt.Verify(model.CurrentPassword, user.UserPassword))
                {
                    SetErrorMessage("Current password is incorrect");
                    return RedirectToAction(nameof(Show));
                }

                user.UserPassword = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
                await _context.SaveChangesAsync();

                SetSuccessMessage("Password updated successfully!");
                return RedirectToAction(nameof(Show));
            }
            catch (Exception ex)
            {
                LogAndSetError(ex, "Failed to update password");
                return RedirectToAction(nameof(Show));
            }
        }

        public async Task<IActionResult> Feedback()
        {
            var userId = GetCurrentUserId();
            var provider = await _context.Providers.FirstOrDefaultAsync(p => p.UserId == userId);
            var feedbacks = await _context.Feedbacks.Where(f => f.ProviderId == provider.ProviderId).OrderByDescending(f => f.FeedbackDate).ToListAsync();

            var averageRating = feedbacks.Any() ? feedbacks.Average(f => f.Rating ?? 0) : 0;
            var totalReviews = feedbacks.Count;

            ViewBag.AverageRating = averageRating;
            ViewBag.TotalReviews = totalReviews;

            return View(feedbacks);
        }
    }
}