using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wasl.Data;
using Wasl.Infrastructure;
using Wasl.ViewModels.BidVMs;
using Wasl.ViewModels.CompanyVMs;
using Wasl.ViewModels.ShipmentVMs;

namespace Wasl.Controllers.Company
{
    [Authorize(Policy = "CompanyOnly")]
    [Route("Company/Bid")]
    public class CompanyBidController : BaseController
    {
        public CompanyBidController(WaslDbContext context, ILogger<CompanyBidController> logger, IFileUploadService fileUpload)
            : base(context, logger, fileUpload) { }

        public async Task<IActionResult> Index(string status = "all", int? shipment_id = null, string search = "")
        {
            var userId = GetCurrentUserId();
            var company = await _context.Companies.FirstOrDefaultAsync(c => c.UserId == userId);

            var query = _context.Bids
                .Where(b => b.ShipmentRequest.CompanyId == company.CompanyId)
                .Include(b => b.ShipmentRequest)
                .Include(b => b.Provider)
                    .ThenInclude(p => p.User)
                .AsQueryable();

            // Filter by status
            if (!string.IsNullOrEmpty(status) && status != "all")
            {
                query = query.Where(b => b.BidStatus == status);
            }

            // Filter by shipment
            if (shipment_id.HasValue)
            {
                query = query.Where(b => b.ShipmentRequestId == shipment_id.Value);
            }

            // Search by provider name
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(b => b.Provider.ProviderName.Contains(search));
            }

            var bids = await query.OrderBy(b => b.BidPrice).ToListAsync();
            var shipments = await _context.ShipmentRequests
                .Where(s => s.CompanyId == company.CompanyId &&
                           (s.Status == AppConstants.SHIPMENT_BIDDING || s.Status == AppConstants.SHIPMENT_ASSIGNED))
                .ToListAsync();

            ViewBag.StatusFilter = status;
            ViewBag.ShipmentFilter = shipment_id;
            ViewBag.SearchTerm = search;

            return View(new CompanyBidsViewModel { Bids = bids, Shipments = shipments });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Accept(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var userId = GetCurrentUserId();
                var company = await _context.Companies.FirstOrDefaultAsync(c => c.UserId == userId);
                var bid = await _context.Bids
                    .Include(b => b.ShipmentRequest)
                    .Include(b => b.Provider)
                    .FirstOrDefaultAsync(b => b.BidId == id && b.ShipmentRequest.CompanyId == company.CompanyId);

                if (bid == null || bid.BidStatus != AppConstants.BID_SUBMITTED)
                {
                    SetErrorMessage("This bid cannot be accepted");
                    return RedirectToBack();
                }

                bid.BidStatus = AppConstants.BID_ACCEPTED;
                bid.ShipmentRequest.Status = AppConstants.SHIPMENT_ASSIGNED;
                bid.ShipmentRequest.ProviderId = bid.ProviderId;
                bid.ShipmentRequest.UpdateAt = DateTime.Now;

                await _context.Bids.Where(b => b.ShipmentRequestId == bid.ShipmentRequestId && b.BidId != bid.BidId)
                    .ForEachAsync(b => b.BidStatus = AppConstants.BID_REJECTED);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                SetSuccessMessage("Bid accepted successfully! Please create a contract for this bid.");
                return RedirectToBack();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                LogAndSetError(ex, "Failed to accept bid");
                return RedirectToBack();
            }
        }

        [HttpGet]
        public async Task<IActionResult> Show(int id)
        {
            var userId = GetCurrentUserId();
            var company = await _context.Companies.FirstOrDefaultAsync(c => c.UserId == userId);

            var bid = await _context.Bids
                .Include(b => b.ShipmentRequest)
                .Include(b => b.Provider)
                    .ThenInclude(p => p.User)
                .Include(b => b.Provider.Feedbacks)
                .Where(b => b.BidId == id && b.ShipmentRequest.CompanyId == company.CompanyId)
                .FirstOrDefaultAsync();

            if (bid == null) return NotFound();

            // Calculate provider rating (like in PHP)
            var providerRating = bid.Provider.Feedbacks?.Any() == true ?
                bid.Provider.Feedbacks.Average(f => f.Rating ?? 0) : 0;
            var totalFeedbacks = bid.Provider.Feedbacks?.Count ?? 0;

            return View(new BidDetailsViewModel
            {
                Bid = bid,
                ProviderRating = providerRating,
                TotalFeedbacks = totalFeedbacks
            });
        }

        [HttpGet]
        public async Task<IActionResult> Shipment(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var company = await _context.Companies.FirstOrDefaultAsync(c => c.UserId == userId);

                var shipment = await _context.ShipmentRequests
                    .Include(s => s.Company)
                    .FirstOrDefaultAsync(s => s.ShipmentRequestId == id && s.CompanyId == company.CompanyId);

                if (shipment == null)
                    return NotFound();

                var bids = await _context.Bids
                    .Include(b => b.Provider)
                        .ThenInclude(p => p.Feedbacks)
                    .Include(b => b.Provider.Contracts)
                    .Where(b => b.ShipmentRequestId == id)
                    .OrderBy(b => b.BidPrice)
                    .ToListAsync();

                var bidStats = new BidStatistics
                {
                    AverageBid = bids.Any() ? bids.Average(b => b.BidPrice ?? 0) : 0,
                    MinBid = bids.Any() ? bids.Min(b => b.BidPrice ?? 0) : 0,
                    MaxBid = bids.Any() ? bids.Max(b => b.BidPrice ?? 0) : 0,
                    ActiveBidders = bids.Select(b => b.ProviderId).Distinct().Count()
                };

                var viewModel = new ShipmentDetailsViewModel
                {
                    Shipment = shipment,
                    Bids = bids,
                    BidStats = bidStats
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                LogAndSetError(ex, "Error loading shipment bids");
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> WaitingContracts(int? shipment_id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var company = await _context.Companies.FirstOrDefaultAsync(c => c.UserId == userId);

                if (company == null)
                    return NotFound();

                // Get all shipments for filter dropdown
                var shipments = await _context.ShipmentRequests
                    .Where(s => s.CompanyId == company.CompanyId)
                    .OrderByDescending(s => s.RequestDate)
                    .ToListAsync();

                ViewBag.Shipments = shipments;

                // Get accepted bids waiting for contracts
                var query = _context.Bids
                    .Include(b => b.Provider)
                        .ThenInclude(p => p.Feedbacks)
                    .Include(b => b.ShipmentRequest)
                    .Where(b => b.ShipmentRequest.CompanyId == company.CompanyId &&
                               b.BidStatus == AppConstants.BID_ACCEPTED);

                // Apply shipment filter if provided
                if (shipment_id.HasValue && shipment_id.Value > 0)
                {
                    query = query.Where(b => b.ShipmentRequestId == shipment_id.Value);
                }

                var bids = await query
                    .OrderByDescending(b => b.SubmitDate)
                    .ToListAsync();

                return View(bids);
            }
            catch (Exception ex)
            {
                LogAndSetError(ex, "Error loading waiting contracts");
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// NEW: Create contract for accepted bid
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> CreateContract(int bidId, IFormFile contractDocument)
        {
            try
            {
                var userId = GetCurrentUserId();
                var company = await _context.Companies.FirstOrDefaultAsync(c => c.UserId == userId);

                if (company == null)
                    return Json(new { success = false, message = "Company not found" });

                var bid = await _context.Bids
                    .Include(b => b.ShipmentRequest)
                    .FirstOrDefaultAsync(b => b.BidId == bidId &&
                                            b.ShipmentRequest.CompanyId == company.CompanyId &&
                                            b.BidStatus == AppConstants.BID_ACCEPTED);

                if (bid == null)
                    return Json(new { success = false, message = "Invalid bid" });

                // Validate file
                if (contractDocument == null || contractDocument.Length == 0)
                    return Json(new { success = false, message = "Contract document is required" });

                var allowedExtensions = new[] { ".pdf", ".doc", ".docx" };
                var extension = Path.GetExtension(contractDocument.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(extension))
                    return Json(new { success = false, message = "Only PDF, DOC, and DOCX files are allowed" });

                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Upload file
                    var filePath = await _fileUploadService.UploadFileAsync(
                        contractDocument,
                        "contracts",
                        $"contract-{bidId}");

                    if (string.IsNullOrEmpty(filePath))
                        return Json(new { success = false, message = "Failed to upload document" });

                    // Create contract
                    var contract = new Models.Contract
                    {
                        BidId = bid.BidId,
                        CompanyId = company.CompanyId,
                        ProviderId = bid.ProviderId,
                        ShipmentId = bid.ShipmentRequestId,
                        ContractDocument = filePath,
                        SignDate = null // Will be set when provider signs
                    };

                    _context.Contracts.Add(contract);
                    bid.BidStatus = AppConstants.BID_CONTRACT_CREATED;

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Json(new
                    {
                        success = true,
                        message = "Contract created successfully! Waiting for provider signature.",
                        redirect = Url.Action("Index", "CompanyContract")
                    });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating contract for bid {BidId}", bidId);
                return Json(new { success = false, message = "Failed to create contract" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string rejection_reason)
        {
            try
            {
                var userId = GetCurrentUserId();
                var company = await _context.Companies.FirstOrDefaultAsync(c => c.UserId == userId);

                var bid = await _context.Bids
                    .Include(b => b.ShipmentRequest)
                    .FirstOrDefaultAsync(b => b.BidId == id &&
                                            b.ShipmentRequest.CompanyId == company.CompanyId);

                if (bid == null)
                {
                    SetErrorMessage("Bid not found");
                    return RedirectToAction(nameof(Index));
                }

                if (bid.BidStatus != AppConstants.BID_SUBMITTED)
                {
                    SetErrorMessage("This bid cannot be rejected");
                    return RedirectToAction(nameof(Index));
                }

                bid.BidStatus = AppConstants.BID_REJECTED;
                bid.BidNotes = rejection_reason ?? "Rejected by company";

                await _context.SaveChangesAsync();

                SetSuccessMessage("Bid rejected successfully");
                return RedirectToBack();
            }
            catch (Exception ex)
            {
                LogAndSetError(ex, "Failed to reject bid");
                return RedirectToBack();
            }
        }

        private IActionResult RedirectToBack()
        {
            string returnUrl = Request.Headers["Referer"].ToString();
            if (string.IsNullOrEmpty(returnUrl))
                return RedirectToAction(nameof(Index));
            return Redirect(returnUrl);
        }
    }
}