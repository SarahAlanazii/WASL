using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wasl.Data;
using Wasl.Infrastructure;
using Wasl.Models;
using Wasl.ViewModels.CompanyVMs;

namespace Wasl.Controllers.Company
{
    /// <summary>
    /// Company Tracking Controller - Track shipments and provide feedback
    /// </summary>
    [RoleAuthorize(AppConstants.ROLE_COMPANY)]
    [Route("Company/Tracking")]
    public class CompanyTrackingController : BaseController
    {
        public CompanyTrackingController(
            WaslDbContext context,
            ILogger<CompanyTrackingController> logger,
            IFileUploadService fileUploadService)
            : base(context, logger, fileUploadService)
        {
        }

        /// <summary>
        /// Show shipment tracking page with contract selection
        /// </summary>
        [HttpGet("")]
        public async Task<IActionResult> Show(int? contractId, string trackingNumber)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                    return RedirectToAction("Login", "Account");

                var company = await _context.Companies
                    .FirstOrDefaultAsync(c => c.UserId == userId.Value);

                if (company == null)
                    return NotFound("Company not found");

                // Get all contracts for dropdown
                var contracts = await _context.Contracts
                    .Where(c => c.CompanyId == company.CompanyId)
                    .Include(c => c.Shipments)
                    .Include(c => c.Provider)
                    .Include(c => c.Bid)
                        .ThenInclude(b => b.ShipmentRequest)
                    .ToListAsync();

                Contract selectedContract = null;
                List<Shipment> shipments = new List<Shipment>();
                Shipment latestShipment = null;
                bool canProvideFeedback = false;
                bool hasProvidedFeedback = false;

                // Search by contract ID
                if (contractId.HasValue)
                {
                    selectedContract = contracts.FirstOrDefault(c => c.ContractId == contractId.Value);

                    if (selectedContract != null)
                    {
                        shipments = await _context.Shipments
                            .Where(s => s.ContractId == selectedContract.ContractId)
                            .OrderBy(s => s.ActualStartDate)
                            .ToListAsync();
                    }
                }
                // Search by tracking number
                else if (!string.IsNullOrWhiteSpace(trackingNumber))
                {
                    var shipment = await _context.Shipments
                        .Include(s => s.Contract)
                            .ThenInclude(c => c.Provider)
                        .Include(s => s.Contract.Bid)
                            .ThenInclude(b => b.ShipmentRequest)
                        .Include(s => s.Feedbacks.Where(f => f.CompanyId == company.CompanyId))
                        .FirstOrDefaultAsync(s => s.TrackingNumber == trackingNumber &&
                            s.Contract.CompanyId == company.CompanyId);

                    if (shipment != null)
                    {
                        selectedContract = shipment.Contract;
                        shipments = await _context.Shipments
                            .Where(s => s.ContractId == selectedContract.ContractId)
                            .OrderBy(s => s.ActualStartDate)
                            .ToListAsync();
                    }
                }

                // Get latest delivered shipment
                if (shipments.Any())
                {
                    latestShipment = shipments
                        .Where(s => s.ActualDeliveryTime.HasValue)
                        .OrderByDescending(s => s.ActualDeliveryTime)
                        .FirstOrDefault();

                    if (latestShipment != null)
                    {
                        var feedbackCount = await _context.Feedbacks
                            .CountAsync(f => f.ShipmentId == latestShipment.ShipmentId &&
                                f.CompanyId == company.CompanyId);

                        hasProvidedFeedback = feedbackCount > 0;
                        canProvideFeedback = latestShipment.CurrentStatus == AppConstants.SHIPMENT_DELIVERED_OK;
                    }
                }

                ViewBag.Contracts = contracts;
                ViewBag.SelectedContract = selectedContract;
                ViewBag.Shipments = shipments;
                ViewBag.LatestShipment = latestShipment;
                ViewBag.CanProvideFeedback = canProvideFeedback;
                ViewBag.HasProvidedFeedback = hasProvidedFeedback;

                return View();
            }
            catch (Exception ex)
            {
                LogAndSetError(ex, "Error loading tracking information");
                return View();
            }
        }

        /// <summary>
        /// Store feedback for completed shipment
        /// </summary>
        [HttpPost("Feedback/{contractId}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StoreFeedback(int contractId, FeedbackViewModel model)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                    return RedirectToAction("Login", "Account");

                var company = await _context.Companies
                    .FirstOrDefaultAsync(c => c.UserId == userId.Value);

                if (company == null)
                    return NotFound("Company not found");

                var contract = await _context.Contracts
                    .Include(c => c.Shipments)
                    .FirstOrDefaultAsync(c => c.ContractId == contractId &&
                        c.CompanyId == company.CompanyId);

                if (contract == null)
                    return NotFound("Contract not found");

                var latestShipment = contract.Shipments
                    .Where(s => s.ActualDeliveryTime.HasValue)
                    .OrderByDescending(s => s.ActualDeliveryTime)
                    .FirstOrDefault();

                if (latestShipment == null || latestShipment.CurrentStatus != AppConstants.SHIPMENT_DELIVERED_OK)
                {
                    SetErrorMessage("Feedback can only be provided for delivered shipments.");
                    return RedirectToAction(nameof(Show), new { contractId });
                }

                // Check if feedback already exists
                var existingFeedback = await _context.Feedbacks
                    .AnyAsync(f => f.ShipmentId == latestShipment.ShipmentId &&
                        f.CompanyId == company.CompanyId);

                if (existingFeedback)
                {
                    SetErrorMessage("You have already provided feedback for this shipment.");
                    return RedirectToAction(nameof(Show), new { contractId });
                }

                if (!ModelState.IsValid)
                {
                    SetErrorMessage("Please provide valid feedback.");
                    return RedirectToAction(nameof(Show), new { contractId });
                }

                var feedback = new Feedback
                {
                    Rating = model.Rating,
                    Comments = model.Comments,
                    FeedbackDate = DateTime.Now,
                    ProviderId = contract.ProviderId,
                    ShipmentId = latestShipment.ShipmentId,
                    CompanyId = company.CompanyId
                };

                _context.Feedbacks.Add(feedback);
                await _context.SaveChangesAsync();

                SetSuccessMessage("Thank you for your feedback! Your review has been submitted successfully.");
                return RedirectToAction(nameof(Show), new { contractId });
            }
            catch (Exception ex)
            {
                LogAndSetError(ex, "Failed to submit feedback. Please try again.");
                return RedirectToAction(nameof(Show), new { contractId });
            }
        }
    }
}
