using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wasl.Data;
using Wasl.Infrastructure;
using Wasl.Models;
using Wasl.ViewModels.CompanyVMs;
using Wasl.ViewModels.ShipmentVMs;

namespace Wasl.Controllers.Company
{
    [Authorize(Policy = "CompanyOnly")]
    public class CompanyShipmentController : BaseController
    {
        public CompanyShipmentController(WaslDbContext context, ILogger<CompanyShipmentController> logger, IFileUploadService fileUpload)
            : base(context, logger, fileUpload) { }

        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            var company = await _context.Companies.FirstOrDefaultAsync(c => c.UserId == userId);
            var shipments = await _context.ShipmentRequests.Where(s => s.CompanyId == company.CompanyId).OrderByDescending(s => s.RequestDate).ToListAsync();

            var shipmentIds = shipments.Select(s => s.ShipmentRequestId).ToList();
            var bidsCounts = await _context.Bids.Where(b => shipmentIds.Contains(b.ShipmentRequestId ?? 0)).GroupBy(b => b.ShipmentRequestId).Select(g => new { ShipmentRequestId = g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.ShipmentRequestId ?? 0, x => x.Count);

            return View(new CompanyShipmentsViewModel { Shipments = shipments, BidsCounts = bidsCounts });
        }

        public IActionResult Create()
        {
            ViewBag.Regions = KSALocations.Regions;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ShipmentRequestViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Regions = KSALocations.Regions;
                return View(model);
            }

            try
            {
                var userId = GetCurrentUserId();
                var company = await _context.Companies.FirstOrDefaultAsync(c => c.UserId == userId);

                var shipment = new ShipmentRequest
                {
                    GoodsType = model.GoodsType,
                    WeightKg = model.WeightKg,
                    PickupLocation = model.PickupLocation,
                    PickupRegion = model.PickupRegion,
                    PickupCity = model.PickupCity,
                    DeliveryLocation = model.DeliveryLocation,
                    DeliveryRegion = model.DeliveryRegion,
                    DeliveryCity = model.DeliveryCity,
                    DeliveryDeadline = model.DeliveryDeadline,
                    SpecialInstructions = model.SpecialInstructions,
                    Status = AppConstants.SHIPMENT_PENDING,
                    RequestDate = DateTime.Now,
                    UpdateAt = DateTime.Now,
                    CompanyId = company.CompanyId
                };

                _context.ShipmentRequests.Add(shipment);
                await _context.SaveChangesAsync();

                SetSuccessMessage("Shipment request created successfully!");
                return RedirectToAction(nameof(Show), new { id = shipment.ShipmentRequestId });
            }
            catch (Exception ex)
            {
                LogAndSetError(ex, "Failed to create shipment");
                ViewBag.Regions = KSALocations.Regions;
                return View(model);
            }
        }

        public async Task<IActionResult> Show(int id)
        {
            var userId = GetCurrentUserId();
            var company = await _context.Companies.FirstOrDefaultAsync(c => c.UserId == userId);
            var shipment = await _context.ShipmentRequests
                .Include(s => s.Bids)
                    .ThenInclude(b => b.Provider)
                        .ThenInclude(p => p.User)
                .Include(s => s.Provider)
                    .ThenInclude(p => p.User)
                .FirstOrDefaultAsync(s => s.ShipmentRequestId == id && s.CompanyId == company.CompanyId);

            if (shipment == null) return NotFound();

            var bids = await _context.Bids.Where(b => b.ShipmentRequestId == id).OrderBy(b => b.BidPrice).ToListAsync();
            return View(new ShipmentDetailsViewModel { Shipment = shipment, Bids = bids });
        }

        /// <summary>
        /// NEW: Show edit form for shipment
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = GetCurrentUserId();
            var company = await _context.Companies.FirstOrDefaultAsync(c => c.UserId == userId);
            var shipment = await _context.ShipmentRequests
                .FirstOrDefaultAsync(s => s.ShipmentRequestId == id && s.CompanyId == company.CompanyId);

            if (shipment == null)
                return NotFound();

            // Check if shipment can be edited
            if (!new[] { AppConstants.SHIPMENT_PENDING, AppConstants.SHIPMENT_BIDDING }.Contains(shipment.Status))
            {
                SetErrorMessage("Shipment cannot be edited in its current status.");
                return RedirectToAction(nameof(Show), new { id });
            }

            var model = new ShipmentRequestViewModel
            {
                GoodsType = shipment.GoodsType,
                WeightKg = shipment.WeightKg ?? 0,
                PickupLocation = shipment.PickupLocation,
                PickupRegion = shipment.PickupRegion,
                PickupCity = shipment.PickupCity,
                DeliveryLocation = shipment.DeliveryLocation,
                DeliveryRegion = shipment.DeliveryRegion,
                DeliveryCity = shipment.DeliveryCity,
                DeliveryDeadline = shipment.DeliveryDeadline ?? DateTime.Now.AddDays(7), // Add default,
                SpecialInstructions = shipment.SpecialInstructions
            };

            ViewBag.Regions = KSALocations.Regions;
            ViewBag.Shipment = shipment;
            return View(model);
        }

        /// <summary>
        /// NEW: Update shipment request
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int id, ShipmentRequestViewModel model)
        {
            var userId = GetCurrentUserId();
            var company = await _context.Companies.FirstOrDefaultAsync(c => c.UserId == userId);
            var shipment = await _context.ShipmentRequests
                .FirstOrDefaultAsync(s => s.ShipmentRequestId == id && s.CompanyId == company.CompanyId);

            if (shipment == null)
                return NotFound();

            // Check if shipment can be edited
            if (!new[] { AppConstants.SHIPMENT_PENDING, AppConstants.SHIPMENT_BIDDING }.Contains(shipment.Status))
            {
                SetErrorMessage("Shipment cannot be edited in its current status.");
                return RedirectToAction(nameof(Show), new { id });
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Regions = KSALocations.Regions;
                ViewBag.Shipment = shipment;
                return View("Edit", model);
            }

            try
            {
                shipment.GoodsType = model.GoodsType;
                shipment.WeightKg = model.WeightKg;
                shipment.PickupLocation = model.PickupLocation;
                shipment.PickupRegion = model.PickupRegion;
                shipment.PickupCity = model.PickupCity;
                shipment.DeliveryLocation = model.DeliveryLocation;
                shipment.DeliveryRegion = model.DeliveryRegion;
                shipment.DeliveryCity = model.DeliveryCity;
                shipment.DeliveryDeadline = model.DeliveryDeadline;
                shipment.SpecialInstructions = model.SpecialInstructions;
                shipment.UpdateAt = DateTime.Now;

                await _context.SaveChangesAsync();

                SetSuccessMessage("Shipment request updated successfully!");
                return RedirectToAction(nameof(Show), new { id });
            }
            catch (Exception ex)
            {
                LogAndSetError(ex, "Failed to update shipment request");
                ViewBag.Regions = KSALocations.Regions;
                ViewBag.Shipment = shipment;
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
                var company = await _context.Companies.FirstOrDefaultAsync(c => c.UserId == userId);
                var shipment = await _context.ShipmentRequests.FirstOrDefaultAsync(s => s.ShipmentRequestId == id && s.CompanyId == company.CompanyId);

                if (shipment == null) return NotFound();

                // Check if shipment can be deleted
                if (!new[] { AppConstants.SHIPMENT_PENDING, AppConstants.SHIPMENT_BIDDING }.Contains(shipment.Status))
                {
                    SetErrorMessage("Shipment cannot be deleted in its current status.");
                    return RedirectToAction(nameof(Show), new { id });
                }

                _context.ShipmentRequests.Remove(shipment);
                await _context.SaveChangesAsync();

                SetSuccessMessage("Shipment deleted successfully!");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                LogAndSetError(ex, "Failed to delete shipment");
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// NEW: Cancel shipment request
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var company = await _context.Companies.FirstOrDefaultAsync(c => c.UserId == userId);
                var shipment = await _context.ShipmentRequests
                    .FirstOrDefaultAsync(s => s.ShipmentRequestId == id && s.CompanyId == company.CompanyId);

                if (shipment == null)
                    return NotFound();

                // Check if shipment can be cancelled
                if (!new[] { AppConstants.SHIPMENT_PENDING, AppConstants.SHIPMENT_BIDDING, AppConstants.SHIPMENT_ASSIGNED }.Contains(shipment.Status))
                {
                    SetErrorMessage("Shipment cannot be cancelled in its current status.");
                    return RedirectToAction(nameof(Show), new { id });
                }

                shipment.Status = AppConstants.SHIPMENT_CANCELLED;
                shipment.UpdateAt = DateTime.Now;

                await _context.SaveChangesAsync();

                SetSuccessMessage("Shipment request cancelled successfully!");
                return RedirectToAction(nameof(Show), new { id });
            }
            catch (Exception ex)
            {
                LogAndSetError(ex, "Failed to cancel shipment request");
                return RedirectToAction(nameof(Show), new { id });
            }
        }

        /// <summary>
        /// NEW: Get cities for a specific region (AJAX)
        /// </summary>
        [HttpGet]
        public JsonResult GetCities(string region)
        {
            if (string.IsNullOrEmpty(region) || !KSALocations.CitiesByRegion.ContainsKey(region))
            {
                return Json(new Dictionary<string, string>());
            }

            var cities = KSALocations.CitiesByRegion[region];
            return Json(cities);
        }
    }
}