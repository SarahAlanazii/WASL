using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wasl.Data;
using Wasl.Infrastructure;
using Wasl.Models;

namespace Wasl.Controllers
{
    [Authorize(Policy = "ProviderOnly")]
    public class ProviderShipmentController : BaseController
    {
        public ProviderShipmentController(WaslDbContext context, ILogger<ProviderShipmentController> logger, IFileUploadService fileUpload)
            : base(context, logger, fileUpload) { }

        public async Task<IActionResult> Index(string status)
        {
            var userId = GetCurrentUserId();
            var provider = await _context.Providers.FirstOrDefaultAsync(p => p.UserId == userId);

            var contractIds = await _context.Contracts.Where(c => c.ProviderId == provider.ProviderId).Select(c => c.ContractId).ToListAsync();
            var query = _context.Shipments.Where(s => contractIds.Contains(s.ContractId ?? 0)).OrderByDescending(s => s.ActualStartDate).AsQueryable();

            if (!string.IsNullOrEmpty(status) && status != "all")
                query = query.Where(s => s.CurrentStatus == status);

            var shipments = await query.ToListAsync();
            return View(shipments);
        }

        public async Task<IActionResult> Create(int contractId)
        {
            var userId = GetCurrentUserId();
            var provider = await _context.Providers.FirstOrDefaultAsync(p => p.UserId == userId);
            var contract = await _context.Contracts.FirstOrDefaultAsync(c => c.ContractId == contractId && c.ProviderId == provider.ProviderId);

            if (contract == null) return NotFound();

            var existingShipment = await _context.Shipments.FirstOrDefaultAsync(s => s.ContractId == contractId);
            if (existingShipment != null)
            {
                SetInfoMessage("Shipment already exists");
                return RedirectToAction(nameof(Show), new { id = existingShipment.ShipmentId });
            }

            return View(contract);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Store(int contractId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var provider = await _context.Providers.FirstOrDefaultAsync(p => p.UserId == userId);
                var contract = await _context.Contracts.FirstOrDefaultAsync(c => c.ContractId == contractId && c.ProviderId == provider.ProviderId);

                if (contract == null) return NotFound();

                var shipment = new Shipment
                {
                    ContractId = contractId,
                    CurrentStatus = AppConstants.SHIPMENT_RECEIVED,
                    ActualStartDate = DateTime.Now,
                    TrackingNumber = $"TRK-{DateTime.Now:yyyyMMdd}-{contractId}"
                };

                _context.Shipments.Add(shipment);
                await _context.SaveChangesAsync();

                SetSuccessMessage($"Shipment created! Tracking: {shipment.TrackingNumber}");
                return RedirectToAction(nameof(Show), new { id = shipment.ShipmentId });
            }
            catch (Exception ex)
            {
                LogAndSetError(ex, "Failed to create shipment");
                return RedirectToAction(nameof(Create), new { contractId });
            }
        }

        public async Task<IActionResult> Show(int id)
        {
            var userId = GetCurrentUserId();
            var provider = await _context.Providers.FirstOrDefaultAsync(p => p.UserId == userId);
            var shipment = await _context.Shipments.Where(s => s.ShipmentId == id && s.Contract.ProviderId == provider.ProviderId).FirstOrDefaultAsync();

            if (shipment == null) return NotFound();

            var statusHistory = await _context.Shipments.Where(s => s.TrackingNumber == shipment.TrackingNumber).OrderBy(s => s.ActualStartDate).ToListAsync();
            ViewBag.StatusHistory = statusHistory;

            return View(shipment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            try
            {
                var userId = GetCurrentUserId();
                var provider = await _context.Providers.FirstOrDefaultAsync(p => p.UserId == userId);
                var currentShipment = await _context.Shipments.FirstOrDefaultAsync(s => s.ShipmentId == id && s.Contract.ProviderId == provider.ProviderId);

                if (currentShipment == null) return NotFound();

                var newShipment = new Shipment
                {
                    ContractId = currentShipment.ContractId,
                    TrackingNumber = currentShipment.TrackingNumber,
                    CurrentStatus = status,
                    ActualStartDate = DateTime.Now,
                    ActualDeliveryTime = status == AppConstants.SHIPMENT_DELIVERED_OK ? DateTime.Now : null
                };

                _context.Shipments.Add(newShipment);
                await _context.SaveChangesAsync();

                SetSuccessMessage($"Status updated to: {status}");
                return RedirectToAction(nameof(Show), new { id = newShipment.ShipmentId });
            }
            catch (Exception ex)
            {
                LogAndSetError(ex, "Failed to update status");
                return RedirectToAction(nameof(Show), new { id });
            }
        }
    }
}