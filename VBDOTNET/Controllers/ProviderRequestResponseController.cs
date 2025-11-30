using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wasl.Data;
using Wasl.Infrastructure;
using Wasl.Models;
using Wasl.ViewModels.ProviderVMs;

namespace Wasl.Controllers
{
    /// <summary>
    /// Provider Request Response Controller - Handle direct shipment requests
    /// </summary>
    [RoleAuthorize(AppConstants.ROLE_PROVIDER)]
    [Route("Provider/Requests")]
    public class ProviderRequestResponseController : BaseController
    {
        public ProviderRequestResponseController(
            WaslDbContext context,
            ILogger<ProviderRequestResponseController> logger,
            IFileUploadService fileUploadService)
            : base(context, logger, fileUploadService)
        {
        }

        /// <summary>
        /// Show direct requests waiting for provider response
        /// </summary>
        [HttpGet("")]
        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                    return RedirectToAction("Login", "Account");

                var provider = await _context.Providers
                    .FirstOrDefaultAsync(p => p.UserId == userId.Value);

                if (provider == null)
                    return NotFound("Provider not found");

                var query = _context.Bids
                    .Include(b => b.ShipmentRequest)
                        .ThenInclude(sr => sr.Company)
                    .Where(b => b.ProviderId == provider.ProviderId &&
                        b.BidStatus == AppConstants.BID_UNDER_REVIEW)
                    .OrderByDescending(b => b.SubmitDate);

                var totalItems = await query.CountAsync();
                var requests = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                ViewBag.TotalItems = totalItems;
                ViewBag.CurrentPage = page;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);

                return View(requests);
            }
            catch (Exception ex)
            {
                LogAndSetError(ex, "Error loading requests");
                return View(new List<Bid>());
            }
        }

        /// <summary>
        /// Show response form
        /// </summary>
        [HttpGet("{id}/Response")]
        public async Task<IActionResult> ShowResponseForm(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                    return RedirectToAction("Login", "Account");

                var provider = await _context.Providers
                    .FirstOrDefaultAsync(p => p.UserId == userId.Value);

                if (provider == null)
                    return NotFound("Provider not found");

                var request = await _context.Bids
                    .Include(b => b.ShipmentRequest)
                        .ThenInclude(sr => sr.Company)
                    .FirstOrDefaultAsync(b => b.BidId == id &&
                        b.ProviderId == provider.ProviderId &&
                        b.BidStatus == AppConstants.BID_UNDER_REVIEW);

                if (request == null)
                    return NotFound("Request not found");

                ViewBag.Request = request;
                return View(new BidResponseViewModel());
            }
            catch (Exception ex)
            {
                LogAndSetError(ex, "Error loading response form");
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Submit bid response to direct request
        /// </summary>
        [HttpPost("{id}/Respond")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Respond(int id, BidResponseViewModel model)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                    return RedirectToAction("Login", "Account");

                var provider = await _context.Providers
                    .FirstOrDefaultAsync(p => p.UserId == userId.Value);

                if (provider == null)
                    return NotFound("Provider not found");

                var directRequest = await _context.Bids
                    .FirstOrDefaultAsync(b => b.BidId == id &&
                        b.ProviderId == provider.ProviderId &&
                        b.BidStatus == AppConstants.BID_UNDER_REVIEW);

                if (directRequest == null)
                    return NotFound("Request not found");

                if (!ModelState.IsValid)
                {
                    var request = await _context.Bids
                        .Include(b => b.ShipmentRequest)
                            .ThenInclude(sr => sr.Company)
                        .FirstOrDefaultAsync(b => b.BidId == id);

                    ViewBag.Request = request;
                    SetErrorMessage("Please provide valid bid information.");
                    return View("ShowResponseForm", model);
                }

                // Update the bid with provider's response
                directRequest.BidPrice = model.BidPrice;
                directRequest.EstimatedDeliveryDays = model.EstimatedDays;
                directRequest.BidNotes = model.BidNotes;
                // Status remains UnderReview - waiting for company decision

                await _context.SaveChangesAsync();

                SetSuccessMessage("Bid submitted successfully! Waiting for company response.");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                LogAndSetError(ex, "Failed to submit bid. Please try again.");
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Decline direct request
        /// </summary>
        [HttpPost("{id}/Decline")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Decline(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                    return RedirectToAction("Login", "Account");

                var provider = await _context.Providers
                    .FirstOrDefaultAsync(p => p.UserId == userId.Value);

                if (provider == null)
                    return NotFound("Provider not found");

                var directRequest = await _context.Bids
                    .FirstOrDefaultAsync(b => b.BidId == id &&
                        b.ProviderId == provider.ProviderId &&
                        b.BidStatus == AppConstants.BID_UNDER_REVIEW);

                if (directRequest == null)
                    return NotFound("Request not found");

                var shipmentRequestId = directRequest.ShipmentRequestId;

                // Delete the bid request
                _context.Bids.Remove(directRequest);
                await _context.SaveChangesAsync();

                // Check if there are any other direct requests for this shipment
                var otherRequests = await _context.Bids
                    .AnyAsync(b => b.ShipmentRequestId == shipmentRequestId &&
                        b.BidStatus == AppConstants.BID_UNDER_REVIEW);

                // If no other direct requests, revert shipment to previous status
                if (!otherRequests)
                {
                    var shipmentRequest = await _context.ShipmentRequests
                        .FindAsync(shipmentRequestId);

                    if (shipmentRequest != null)
                    {
                        shipmentRequest.Status = AppConstants.SHIPMENT_PENDING;
                        shipmentRequest.UpdateAt = DateTime.Now;
                        await _context.SaveChangesAsync();
                    }
                }

                SetSuccessMessage("Direct request declined successfully.");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                LogAndSetError(ex, "Failed to decline request. Please try again.");
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
