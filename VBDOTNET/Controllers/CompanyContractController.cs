using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wasl.Data;
using Wasl.Infrastructure;
using Wasl.Models;
using Wasl.ViewModels;
using Wasl.ViewModels.CompanyVMs;

namespace Wasl.Controllers.Company
{
    [Authorize(Policy = "CompanyOnly")]
    public class CompanyContractController : BaseController
    {
        public CompanyContractController(WaslDbContext context, ILogger<CompanyContractController> logger, IFileUploadService fileUpload)
            : base(context, logger, fileUpload) { }

        // GET: CompanyContract/Index
        public async Task<IActionResult> Index(string shipment_id = "all")
        {
            var userId = GetCurrentUserId();
            var company = await _context.Companies.FirstOrDefaultAsync(c => c.UserId == userId);

            if (company == null) return NotFound();

            var query = _context.Contracts
                .Include(c => c.Bid)
                    .ThenInclude(b => b.ShipmentRequest)
                .Include(c => c.Provider)
                .Include(c => c.Invoices)
                    .ThenInclude(i => i.Payments)
                .Include(c => c.Shipment)
                .Where(c => c.CompanyId == company.CompanyId)
                .Where(c => c.Invoices.Any(i => i.Payments.Any(p => p.PaymentStatus == AppConstants.PAYMENT_SUCCESSFUL)));

            // Filter by shipment if provided
            if (!string.IsNullOrEmpty(shipment_id) && shipment_id != "all")
            {
                if (int.TryParse(shipment_id, out int shipmentId))
                {
                    query = query.Where(c => c.Bid.ShipmentRequest.ShipmentRequestId == shipmentId);
                }
            }

            var contracts = await query.ToListAsync();

            // Fix: Get shipments that have contracts through bids
            var shipments = await _context.ShipmentRequests
                .Where(sr => sr.CompanyId == company.CompanyId)
                .Where(sr => _context.Bids.Any(b => b.ShipmentRequestId == sr.ShipmentRequestId &&
                                                   _context.Contracts.Any(c => c.BidId == b.BidId)))
                .ToListAsync();

            ViewBag.Shipments = shipments;
            ViewBag.Company = company;
            return View(contracts);
        }

        // GET: CompanyContract/WaitingPayment
        public async Task<IActionResult> WaitingPayment(string shipment_id = "all")
        {
            var userId = GetCurrentUserId();
            var company = await _context.Companies.FirstOrDefaultAsync(c => c.UserId == userId);

            if (company == null) return NotFound();

            var query = _context.Contracts
                .Include(c => c.Bid)
                    .ThenInclude(b => b.ShipmentRequest)
                .Include(c => c.Provider)
                .Include(c => c.Invoices)
                    .ThenInclude(i => i.Payments)
                .Where(c => c.CompanyId == company.CompanyId)
                .Where(c => !c.Invoices.Any(i => i.Payments.Any(p => p.PaymentStatus == AppConstants.PAYMENT_SUCCESSFUL)));

            // Filter by shipment
            if (!string.IsNullOrEmpty(shipment_id) && shipment_id != "all")
            {
                if (int.TryParse(shipment_id, out int shipmentId))
                {
                    query = query.Where(c => c.Bid.ShipmentRequest.ShipmentRequestId == shipmentId);
                }
            }

            var contracts = await query.ToListAsync();

            // Fix: Get shipments that have contracts through bids
            var shipments = await _context.ShipmentRequests
                .Where(sr => sr.CompanyId == company.CompanyId)
                .Where(sr => _context.Bids.Any(b => b.ShipmentRequestId == sr.ShipmentRequestId &&
                                                   _context.Contracts.Any(c => c.BidId == b.BidId)))
                .ToListAsync();

            ViewBag.Shipments = shipments;

            var viewModel = new CompanyContractsViewModel
            {
                Contracts = contracts,
                StatusFilter = "waiting_payment"
            };

            return View(viewModel);
        }

        // GET: CompanyContract/Show/5
        public async Task<IActionResult> Show(int id)
        {
            var userId = GetCurrentUserId();
            var company = await _context.Companies.FirstOrDefaultAsync(c => c.UserId == userId);

            if (company == null) return NotFound();

            var contract = await _context.Contracts
                .Include(c => c.Bid)
                    .ThenInclude(b => b.ShipmentRequest)
                .Include(c => c.Provider)
                .Include(c => c.Invoices)
                    .ThenInclude(i => i.Payments)
                .Include(c => c.Shipment)
                .FirstOrDefaultAsync(c => c.ContractId == id && c.CompanyId == company.CompanyId);

            if (contract == null) return NotFound();

            var shipment = await _context.Shipments
                .Where(s => s.ContractId == id)
                .OrderByDescending(s => s.ActualStartDate)
                .FirstOrDefaultAsync();

            return View(new ContractDetailsViewModel { Contract = contract, Shipment = shipment });
        }

        // GET: CompanyContract/ViewInvoice/5
        public async Task<IActionResult> ViewInvoice(int id)
        {
            var userId = GetCurrentUserId();
            var company = await _context.Companies.FirstOrDefaultAsync(c => c.UserId == userId);

            if (company == null) return NotFound();

            var invoice = await _context.Invoices
                .Include(i => i.Contract)
                    .ThenInclude(c => c.Bid)
                        .ThenInclude(b => b.ShipmentRequest)
                .Include(i => i.Contract)
                    .ThenInclude(c => c.Provider)
                .Include(i => i.Payments)
                .FirstOrDefaultAsync(i => i.InvoiceId == id && i.Contract.CompanyId == company.CompanyId);

            if (invoice == null) return NotFound();

            return View("~/Views/Company/Contracts/Modals/InvoiceDetails.cshtml", invoice);
        }

        // POST: CompanyContract/ProcessPayment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> ProcessPayment(int contractId, PaymentViewModel model)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var userId = GetCurrentUserId();
                var company = await _context.Companies.FirstOrDefaultAsync(c => c.UserId == userId);

                if (company == null) return JsonError("Company not found");

                var contract = await _context.Contracts
                    .Include(c => c.Bid)
                        .ThenInclude(b => b.ShipmentRequest)
                    .Include(c => c.Invoices)
                    .FirstOrDefaultAsync(c => c.ContractId == contractId && c.CompanyId == company.CompanyId);

                if (contract == null) return JsonError("Contract not found");
                if (contract.Bid == null) return JsonError("Bid not found");

                // Validate payment model
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return JsonError("Validation failed", errors);
                }

                // Check if invoice already exists for this contract
                var existingInvoice = contract.Invoices?.FirstOrDefault();

                Invoice invoice;
                if (existingInvoice == null)
                {
                    // Create new invoice
                    invoice = new Invoice
                    {
                        InvoiceNumber = $"INV-{DateTime.Now:yyyyMMdd}-{contractId}",
                        InvoiceDate = DateTime.Now,
                        InvoiceDueDate = DateTime.Now.AddDays(15),
                        ContractId = contractId
                    };
                    _context.Invoices.Add(invoice);
                    await _context.SaveChangesAsync(); // Save to get InvoiceId
                }
                else
                {
                    invoice = existingInvoice;
                }

                // Create payment
                var payment = new Payment
                {
                    PaymentAmount = contract.Bid.BidPrice ?? 0,
                    PaymentMethod = model?.PaymentMethod ?? "credit_card",
                    PaymentStatus = AppConstants.PAYMENT_SUCCESSFUL,
                    TransactionId = $"TXN-{DateTime.Now.Ticks}",
                    PaymentDate = DateTime.Now,
                    InvoiceId = invoice.InvoiceId
                };

                _context.Payments.Add(payment);

                // Update invoice with payment reference
                invoice.PaymentId = payment.PaymentId;

                // Update shipment request status
                if (contract.Bid.ShipmentRequest != null)
                {
                    contract.Bid.ShipmentRequest.Status = AppConstants.SHIPMENT_IN_PROGRESS;
                    contract.Bid.ShipmentRequest.UpdateAt = DateTime.Now;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new
                {
                    success = true,
                    message = "Payment processed successfully! Your shipment is now in progress.",
                    redirect = Url.Action("WaitingPayment", "CompanyContract")
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error processing payment for contract {ContractId}", contractId);
                return JsonError("Payment failed");
            }
        }

        // GET: CompanyContract/DownloadContract/5
        public async Task<IActionResult> DownloadContract(int id)
        {
            var userId = GetCurrentUserId();
            var company = await _context.Companies.FirstOrDefaultAsync(c => c.UserId == userId);

            if (company == null) return NotFound();

            var contract = await _context.Contracts
                .FirstOrDefaultAsync(c => c.ContractId == id && c.CompanyId == company.CompanyId);

            if (contract == null || string.IsNullOrEmpty(contract.ContractDocument))
                return NotFound();

            var filePath = contract.ContractDocument;
            if (!System.IO.File.Exists(filePath))
                return NotFound();

            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            var fileName = Path.GetFileName(filePath);

            return File(fileBytes, "application/octet-stream", fileName);
        }

        // GET: CompanyContract/DownloadInvoice/5
        public async Task<IActionResult> DownloadInvoice(int id)
        {
            var userId = GetCurrentUserId();
            var company = await _context.Companies.FirstOrDefaultAsync(c => c.UserId == userId);

            if (company == null) return NotFound();

            var invoice = await _context.Invoices
                .Include(i => i.Contract)
                .FirstOrDefaultAsync(i => i.InvoiceId == id && i.Contract.CompanyId == company.CompanyId);

            if (invoice == null) return NotFound();

            // For now, return a simple PDF generation or redirect to view
            // In a real application, you would generate and return a PDF file
            return RedirectToAction("ViewInvoice", new { id });
        }

        // POST: CompanyContract/CreateContract
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateContract(int bidId, IFormFile contractDocument)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
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

                // Upload file
                var filePath = await _fileUploadService.UploadFileAsync(
                    contractDocument,
                    "contracts",
                    $"contract-{bidId}");

                if (string.IsNullOrEmpty(filePath))
                    return Json(new { success = false, message = "Failed to upload document" });

                // Create contract
                var contract = new Contract
                {
                    BidId = bid.BidId,
                    CompanyId = company.CompanyId,
                    ProviderId = bid.ProviderId,
                    ShipmentId = bid.ShipmentRequestId,
                    ContractDocument = filePath,
                    SignDate = DateTime.Now
                };

                _context.Contracts.Add(contract);
                bid.BidStatus = AppConstants.BID_CONTRACT_CREATED;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new
                {
                    success = true,
                    message = "Contract created successfully!",
                    redirect = Url.Action("Index", "CompanyContract")
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating contract for bid {BidId}", bidId);
                return Json(new { success = false, message = "Failed to create contract" });
            }
        }

        // Helper methods
        private JsonResult JsonError(string message, List<string> errors = null)
        {
            return Json(new
            {
                success = false,
                message = message,
                errors = errors
            });
        }

        private JsonResult JsonSuccess(string message, object data = null)
        {
            return Json(new
            {
                success = true,
                message = message,
                data = data
            });
        }
    }
}