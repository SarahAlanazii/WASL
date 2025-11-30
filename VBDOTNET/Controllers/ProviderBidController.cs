using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wasl.Data;
using Wasl.Infrastructure;
using Wasl.Models;
using Wasl.ViewModels;
using Wasl.ViewModels.BidVMs;

namespace Wasl.Controllers
{
    [Authorize(Policy = "ProviderOnly")]
    public class ProviderBidController : BaseController
    {
        public ProviderBidController(WaslDbContext context, ILogger<ProviderBidController> logger, IFileUploadService fileUpload)
            : base(context, logger, fileUpload) { }

        public async Task<IActionResult> Index(string status)
        {
            var userId = GetCurrentUserId();
            var provider = await _context.Providers.FirstOrDefaultAsync(p => p.UserId == userId);

            var query = _context.Bids.Where(b => b.ProviderId == provider.ProviderId).OrderByDescending(b => b.SubmitDate).AsQueryable();

            if (!string.IsNullOrEmpty(status) && status != "all")
                query = query.Where(b => b.BidStatus == status);

            var bids = await query.ToListAsync();
            return View(bids);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var userId = GetCurrentUserId();
            var provider = await _context.Providers.FirstOrDefaultAsync(p => p.UserId == userId);
            var bid = await _context.Bids.FirstOrDefaultAsync(b => b.BidId == id && b.ProviderId == provider.ProviderId);

            if (bid == null) return NotFound();

            return View(new BidViewModel
            {
                ShipmentRequestId = bid.ShipmentRequestId ?? 0,
                BidPrice = bid.BidPrice ?? 0,
                EstimatedDeliveryDays = bid.EstimatedDeliveryDays ?? 0,
                BidNotes = bid.BidNotes
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int id, BidViewModel model)
        {
            if (!ModelState.IsValid) return View("Edit", model);

            try
            {
                var userId = GetCurrentUserId();
                var provider = await _context.Providers.FirstOrDefaultAsync(p => p.UserId == userId);
                var bid = await _context.Bids.FirstOrDefaultAsync(b => b.BidId == id && b.ProviderId == provider.ProviderId);

                if (bid == null || bid.BidStatus != AppConstants.BID_SUBMITTED)
                {
                    SetErrorMessage("This bid cannot be updated");
                    return RedirectToAction(nameof(Index));
                }

                bid.BidPrice = model.BidPrice;
                bid.EstimatedDeliveryDays = model.EstimatedDeliveryDays;
                bid.BidNotes = model.BidNotes;
                await _context.SaveChangesAsync();

                SetSuccessMessage("Bid updated successfully!");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                LogAndSetError(ex, "Failed to update bid");
                return View("Edit", model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var provider = await _context.Providers.FirstOrDefaultAsync(p => p.UserId == userId);
                var bid = await _context.Bids.FirstOrDefaultAsync(b => b.BidId == id && b.ProviderId == provider.ProviderId);

                if (bid == null)
                {
                    SetErrorMessage("Bid not found");
                    return RedirectToAction(nameof(Index));
                }

                _context.Bids.Remove(bid);
                await _context.SaveChangesAsync();

                SetSuccessMessage("Bid cancelled successfully");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                LogAndSetError(ex, "Failed to cancel bid");
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Store(BidViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction("ShipmentDetails", "Front", new { id = model.ShipmentRequestId });
            }

            try
            {
                var userId = GetCurrentUserId();
                var provider = await _context.Providers.FirstOrDefaultAsync(p => p.UserId == userId);

                if (provider == null)
                {
                    SetErrorMessage("Provider profile not found");
                    return RedirectToAction("ShipmentDetails", "Front", new { id = model.ShipmentRequestId });
                }

                // Check if shipment exists and is available for bidding
                var shipment = await _context.ShipmentRequests
                    .Where(s => s.ShipmentRequestId == model.ShipmentRequestId &&
                               (s.Status == AppConstants.SHIPMENT_PENDING ||
                                s.Status == AppConstants.SHIPMENT_BIDDING))
                    .FirstOrDefaultAsync();

                if (shipment == null)
                {
                    SetErrorMessage("This shipment is no longer available for bidding.");
                    return RedirectToAction("ShipmentDetails", "Front", new { id = model.ShipmentRequestId });
                }

                // Check for existing bid
                var existingBid = await _context.Bids
                    .Where(b => b.ShipmentRequestId == model.ShipmentRequestId &&
                               b.ProviderId == provider.ProviderId)
                    .FirstOrDefaultAsync();

                if (existingBid != null)
                {
                    TempData["existing_bid_id"] = existingBid.BidId;
                    SetErrorMessage("You have already submitted a bid for this shipment.");
                    return RedirectToAction("ShipmentDetails", "Front", new { id = model.ShipmentRequestId });
                }

                // Create the bid
                var bid = new Bid
                {
                    ShipmentRequestId = model.ShipmentRequestId,
                    ProviderId = provider.ProviderId,
                    BidPrice = model.BidPrice,
                    EstimatedDeliveryDays = model.EstimatedDeliveryDays,
                    BidNotes = model.BidNotes,
                    BidStatus = AppConstants.BID_SUBMITTED,
                    SubmitDate = DateTime.Now
                };

                _context.Bids.Add(bid);
                await _context.SaveChangesAsync();

                // Update shipment status if needed
                if (shipment.Status == AppConstants.SHIPMENT_PENDING)
                {
                    shipment.Status = AppConstants.SHIPMENT_BIDDING;
                    shipment.UpdateAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                }

                SetSuccessMessage("Your bid has been submitted successfully!");
                return RedirectToAction("ShipmentDetails", "Front", new { id = model.ShipmentRequestId });
            }
            catch (Exception ex)
            {
                LogAndSetError(ex, "Failed to submit your bid. Please try again.");
                return RedirectToAction("ShipmentDetails", "Front", new { id = model.ShipmentRequestId });
            }
        }
    }
}