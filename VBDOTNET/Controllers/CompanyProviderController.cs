using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wasl.Data;
using Wasl.Models;
using Wasl.ViewModels.CompanyVMs;
using System.Linq.Dynamic.Core;
using Wasl.Infrastructure;

namespace Wasl.Controllers.Company
{
    [Authorize(Policy = "CompanyOnly")]
    public class CompanyProviderController : BaseController
    {
        public CompanyProviderController(WaslDbContext context, ILogger<CompanyProviderController> logger, IFileUploadService fileUpload)
            : base(context, logger, fileUpload) { }

        /// <summary>
        /// Display all approved providers
        /// </summary>
        public async Task<IActionResult> Index(string search = "", string region = "all", string rating = "all", string sort = "name", int page = 1)
        {
            try
            {
                var query = _context.Providers
                    .Where(p => p.IsApproved == true)
                    .Include(p => p.User)
                    .Include(p => p.Feedbacks)
                    .AsQueryable();

                // Search by name or location
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(p => p.ProviderName.Contains(search) ||
                                           p.ProviderCity.Contains(search) ||
                                           p.ProviderRegion.Contains(search));
                }

                // Filter by region
                if (!string.IsNullOrEmpty(region) && region != "all")
                {
                    query = query.Where(p => p.ProviderRegion == region);
                }

                // Filter by rating
                if (!string.IsNullOrEmpty(rating) && rating != "all")
                {
                    if (int.TryParse(rating, out int minRating))
                    {
                        query = query.Where(p => p.Feedbacks.Average(f => f.Rating ?? 0) >= minRating &&
                                               p.Feedbacks.Average(f => f.Rating ?? 0) < minRating + 1);
                    }
                }

                // Get counts before pagination
                var totalItems = await query.CountAsync();

                // Sort options
                switch (sort)
                {
                    case "rating":
                        query = query.OrderByDescending(p => p.Feedbacks.Average(f => f.Rating ?? 0));
                        break;
                    case "projects":
                        query = query.OrderByDescending(p => p.Contracts.Count);
                        break;
                    case "name":
                    default:
                        query = query.OrderBy(p => p.ProviderName);
                        break;
                }

                // Pagination
                var pageSize = 12;
                var providers = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // Calculate additional data
                var providersWithStats = providers.Select(p => new
                {
                    Provider = p,
                    AverageRating = p.Feedbacks.Any() ? p.Feedbacks.Average(f => f.Rating ?? 0) : 0,
                    FeedbackCount = p.Feedbacks.Count,
                    ProjectCount = p.Contracts.Count
                }).ToList();

                ViewBag.Regions = KSALocations.Regions;
                ViewBag.Search = search;
                ViewBag.SelectedRegion = region;
                ViewBag.SelectedRating = rating;
                ViewBag.Sort = sort;
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
                ViewBag.TotalItems = totalItems;

                return View(providersWithStats);
            }
            catch (Exception ex)
            {
                LogAndSetError(ex, "Error loading providers");
                return View(new List<object>());
            }
        }

        /// <summary>
        /// Show provider profile
        /// </summary>
        public async Task<IActionResult> Show(int id)
        {
            try
            {
                var provider = await _context.Providers
                    .Where(p => p.IsApproved == true)
                    .Include(p => p.User)
                    .Include(p => p.Feedbacks)
                        .ThenInclude(f => f.Company)
                            .ThenInclude(c => c.User)
                    .Include(p => p.Contracts)
                        .ThenInclude(c => c.Shipment)
                    .FirstOrDefaultAsync(p => p.ProviderId == id);

                if (provider == null)
                {
                    SetErrorMessage("Provider not found");
                    return RedirectToAction(nameof(Index));
                }

                // Calculate average rating
                var averageRating = provider.Feedbacks.Any() ?
                    provider.Feedbacks.Average(f => f.Rating ?? 0) : 0;
                var ratingCount = provider.Feedbacks.Count;

                // Get recent completed shipments
                var completedShipments = await _context.Contracts
                    .Where(c => c.ProviderId == id &&
                               c.Shipments.Any(s => s.CurrentStatus == AppConstants.SHIPMENT_DELIVERED)) // Change to Shipments
                    .Include(c => c.Shipments)
                    .Take(5)
                    .ToListAsync();

                // Check if company has active shipments that can be sent to this provider
                var userId = GetCurrentUserId();
                var company = await _context.Companies.FirstOrDefaultAsync(c => c.UserId == userId);

                var availableShipments = await _context.ShipmentRequests
                    .Where(s => s.CompanyId == company.CompanyId &&
                               s.Status == AppConstants.SHIPMENT_PENDING)
                    .ToListAsync();

                var viewModel = new ProviderDetailsViewModel
                {
                    Provider = provider,
                    AverageRating = averageRating,
                    RatingCount = ratingCount,
                    CompletedShipments = completedShipments,
                    AvailableShipments = availableShipments
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                LogAndSetError(ex, "Error loading provider details");
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Send direct shipment request to provider
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendRequest(int providerId, SendRequestViewModel model)
        {
            try
            {
                var userId = GetCurrentUserId();
                var company = await _context.Companies.FirstOrDefaultAsync(c => c.UserId == userId);
                var provider = await _context.Providers
                    .Where(p => p.IsApproved == true)
                    .FirstOrDefaultAsync(p => p.ProviderId == providerId);

                if (company == null || provider == null)
                {
                    SetErrorMessage("Company or provider not found");
                    return RedirectToAction(nameof(Index));
                }

                var shipment = await _context.ShipmentRequests
                    .FirstOrDefaultAsync(s => s.ShipmentRequestId == model.ShipmentId &&
                                             s.CompanyId == company.CompanyId);

                if (shipment == null)
                {
                    SetErrorMessage("Shipment not found");
                    return RedirectToAction(nameof(Show), new { id = providerId });
                }

                // Check if shipment is available for direct request
                if (!new[] { AppConstants.SHIPMENT_PENDING, AppConstants.SHIPMENT_BIDDING }.Contains(shipment.Status))
                {
                    SetErrorMessage("This shipment is not available for direct requests.");
                    return RedirectToAction(nameof(Show), new { id = providerId });
                }

                // Check if provider already has a request/bid for this shipment
                var existingRequest = await _context.Bids
                    .AnyAsync(b => b.ShipmentRequestId == shipment.ShipmentRequestId &&
                                  b.ProviderId == providerId &&
                                  b.BidStatus == AppConstants.BID_UNDER_REVIEW);

                if (existingRequest)
                {
                    SetErrorMessage("A direct request already exists for this provider.");
                    return RedirectToAction(nameof(Show), new { id = providerId });
                }

                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Create a direct request bid
                    var bid = new Bid
                    {
                        ShipmentRequestId = shipment.ShipmentRequestId,
                        ProviderId = providerId,
                        BidPrice = 0, // To be filled by provider
                        EstimatedDeliveryDays = 0, // To be filled by provider
                        BidNotes = model.Message ?? "Direct request from company",
                        BidStatus = AppConstants.BID_UNDER_REVIEW,
                        SubmitDate = DateTime.Now
                    };

                    _context.Bids.Add(bid);

                    // Update shipment status to DirectRequest to hide from other providers
                    shipment.Status = AppConstants.SHIPMENT_DIRECT_REQUEST;
                    shipment.UpdateAt = DateTime.Now;

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    SetSuccessMessage("Direct request sent successfully! The provider will review your request.");
                    return RedirectToAction(nameof(Show), new { id = providerId });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                LogAndSetError(ex, "Failed to send direct request. Please try again.");
                return RedirectToAction(nameof(Show), new { id = providerId });
            }
        }

        /// <summary>
        /// Display direct requests sent by company
        /// </summary>
        public async Task<IActionResult> DirectRequests(int? provider_id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var company = await _context.Companies.FirstOrDefaultAsync(c => c.UserId == userId);

                if (company == null)
                {
                    SetErrorMessage("Company not found");
                    return RedirectToAction(nameof(Index));
                }

                var query = _context.Bids
                    .Where(b => b.ShipmentRequest.CompanyId == company.CompanyId &&
                               b.BidStatus == AppConstants.BID_UNDER_REVIEW)
                    .Include(b => b.ShipmentRequest)
                    .Include(b => b.Provider)
                        .ThenInclude(p => p.User)
                    .AsQueryable();

                // Filter by provider
                if (provider_id.HasValue)
                {
                    query = query.Where(b => b.ProviderId == provider_id.Value);
                }

                var directRequests = await query.ToListAsync();
                var providers = await _context.Providers
                    .Where(p => p.IsApproved == true)
                    .ToListAsync();

                var viewModel = new DirectRequestsViewModel
                {
                    DirectRequests = directRequests,
                    Providers = providers
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                LogAndSetError(ex, "Error loading direct requests");
                return View(new DirectRequestsViewModel());
            }
        }

        /// <summary>
        /// Delete a sent direct request
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRequest(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var company = await _context.Companies.FirstOrDefaultAsync(c => c.UserId == userId);

                if (company == null)
                {
                    SetErrorMessage("Company not found");
                    return RedirectToAction(nameof(DirectRequests));
                }

                var bid = await _context.Bids
                    .Where(b => b.BidId == id &&
                               b.ShipmentRequest.CompanyId == company.CompanyId &&
                               b.BidStatus == AppConstants.BID_UNDER_REVIEW)
                    .FirstOrDefaultAsync();

                if (bid == null)
                {
                    SetErrorMessage("Direct request not found");
                    return RedirectToAction(nameof(DirectRequests));
                }

                _context.Bids.Remove(bid);
                await _context.SaveChangesAsync();

                SetSuccessMessage("Direct request deleted successfully.");
                return RedirectToAction(nameof(DirectRequests));
            }
            catch (Exception ex)
            {
                LogAndSetError(ex, "Failed to delete direct request. Please try again.");
                return RedirectToAction(nameof(DirectRequests));
            }
        }

        /// <summary>
        /// Accept provider's bid from direct request
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Accept(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var company = await _context.Companies.FirstOrDefaultAsync(c => c.UserId == userId);

                if (company == null)
                {
                    SetErrorMessage("Company not found");
                    return RedirectToAction(nameof(DirectRequests));
                }

                var bid = await _context.Bids
                    .Where(b => b.BidId == id &&
                               b.ShipmentRequest.CompanyId == company.CompanyId &&
                               b.BidStatus == AppConstants.BID_UNDER_REVIEW &&
                               b.BidPrice > 0)
                    .FirstOrDefaultAsync();

                if (bid == null)
                {
                    SetErrorMessage("Bid not found or invalid");
                    return RedirectToAction(nameof(DirectRequests));
                }

                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Update bid status to Accepted
                    bid.BidStatus = AppConstants.BID_ACCEPTED;

                    // Update shipment status to Assigned
                    var shipment = await _context.ShipmentRequests
                        .FirstOrDefaultAsync(s => s.ShipmentRequestId == bid.ShipmentRequestId);

                    if (shipment != null)
                    {
                        shipment.Status = AppConstants.SHIPMENT_ASSIGNED;
                        shipment.ProviderId = bid.ProviderId;
                        shipment.UpdateAt = DateTime.Now;
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    SetSuccessMessage("Bid accepted successfully! Contract created.");
                    return RedirectToAction(nameof(DirectRequests));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                LogAndSetError(ex, "Failed to accept bid. Please try again.");
                return RedirectToAction(nameof(DirectRequests));
            }
        }

        /// <summary>
        /// Reject provider's bid from direct request
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var company = await _context.Companies.FirstOrDefaultAsync(c => c.UserId == userId);

                if (company == null)
                {
                    SetErrorMessage("Company not found");
                    return RedirectToAction(nameof(DirectRequests));
                }

                var bid = await _context.Bids
                    .Where(b => b.BidId == id &&
                               b.ShipmentRequest.CompanyId == company.CompanyId &&
                               b.BidStatus == AppConstants.BID_UNDER_REVIEW &&
                               b.BidPrice > 0)
                    .FirstOrDefaultAsync();

                if (bid == null)
                {
                    SetErrorMessage("Bid not found or invalid");
                    return RedirectToAction(nameof(DirectRequests));
                }

                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Update bid status to Rejected
                    bid.BidStatus = AppConstants.BID_REJECTED;

                    // Revert shipment back to Pending for new requests
                    var shipment = await _context.ShipmentRequests
                        .FirstOrDefaultAsync(s => s.ShipmentRequestId == bid.ShipmentRequestId);

                    if (shipment != null)
                    {
                        shipment.Status = AppConstants.SHIPMENT_PENDING;
                        shipment.UpdateAt = DateTime.Now;
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    SetSuccessMessage("Bid rejected successfully.");
                    return RedirectToAction(nameof(DirectRequests));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                LogAndSetError(ex, "Failed to reject bid. Please try again.");
                return RedirectToAction(nameof(DirectRequests));
            }
        }
    }
}