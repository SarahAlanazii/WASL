using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wasl.Data;
using Wasl.Infrastructure;
using Wasl.Models;
using Wasl.ViewModels.InvoiceVMs;

namespace Wasl.Controllers.Company
{
    /// <summary>
    /// Company Invoice Controller - Manages invoice viewing and downloads
    /// </summary>
    [RoleAuthorize(AppConstants.ROLE_COMPANY)]
    [Route("Company/Invoices")]
    public class CompanyInvoiceController : BaseController
    {
        public CompanyInvoiceController(
            WaslDbContext context,
            ILogger<CompanyInvoiceController> logger,
            IFileUploadService fileUploadService)
            : base(context, logger, fileUploadService)
        {
        }

        /// <summary>
        /// Display all invoices for company
        /// </summary>
        [HttpGet("")]
        public async Task<IActionResult> Index(string status = "all")
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

                var query = _context.Invoices
                    .Include(i => i.Contract)
                        .ThenInclude(c => c.Bid)
                            .ThenInclude(b => b.ShipmentRequest)
                    .Include(i => i.Contract.Provider)
                    .Include(i => i.Payment)
                    .Where(i => i.Contract.CompanyId == company.CompanyId);

                // Filter by payment status
                if (status != "all")
                {
                    switch (status)
                    {
                        case "paid":
                            query = query.Where(i => i.Payment != null &&
                                i.Payment.PaymentStatus == AppConstants.PAYMENT_SUCCESSFUL);
                            break;
                        case "unpaid":
                            query = query.Where(i => i.Payment == null ||
                                i.Payment.PaymentStatus != AppConstants.PAYMENT_SUCCESSFUL);
                            break;
                        case "overdue":
                            query = query.Where(i => i.InvoiceDueDate < DateTime.Now &&
                                (i.Payment == null ||
                                 i.Payment.PaymentStatus != AppConstants.PAYMENT_SUCCESSFUL));
                            break;
                    }
                }

                var invoices = await query
                    .OrderByDescending(i => i.InvoiceDate)
                    .ToListAsync();

                var viewModel = new InvoicesListViewModel
                {
                    Invoices = invoices,
                    StatusFilter = status
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                LogAndSetError(ex, "Error loading invoices");
                return View(new InvoicesListViewModel { Invoices = new List<Invoice>() });
            }
        }

        /// <summary>
        /// Show invoice details
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> Show(int id)
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

                var invoice = await _context.Invoices
                    .Include(i => i.Contract)
                        .ThenInclude(c => c.Bid)
                            .ThenInclude(b => b.ShipmentRequest)
                    .Include(i => i.Contract.Provider)
                    .Include(i => i.Payment)
                    .FirstOrDefaultAsync(i => i.InvoiceId == id &&
                        i.Contract.CompanyId == company.CompanyId);

                if (invoice == null)
                    return NotFound("Invoice not found");

                var viewModel = new InvoiceViewModel
                {
                    Invoice = invoice,
                    Contract = invoice.Contract,
                    Payment = invoice.Payment
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                LogAndSetError(ex, "Error loading invoice details");
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Get invoice modal content (AJAX)
        /// </summary>
        [HttpGet("Modal/{id}")]
        public async Task<IActionResult> Modal(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                    return Unauthorized();

                var company = await _context.Companies
                    .FirstOrDefaultAsync(c => c.UserId == userId.Value);

                if (company == null)
                    return NotFound();

                var invoice = await _context.Invoices
                    .Include(i => i.Contract)
                        .ThenInclude(c => c.Bid)
                            .ThenInclude(b => b.ShipmentRequest)
                    .Include(i => i.Contract.Provider)
                    .Include(i => i.Contract.Company)
                        .ThenInclude(c => c.User)
                    .Include(i => i.Payment)
                    .FirstOrDefaultAsync(i => i.InvoiceId == id &&
                        i.Contract.CompanyId == company.CompanyId);

                if (invoice == null)
                    return NotFound();

                return PartialView("_InvoiceModalContent", invoice);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                return StatusCode(500);
            }
        }

        /// <summary>
        /// Download invoice as text file (placeholder for PDF generation)
        /// </summary>
        [HttpGet("Download/{id}")]
        public async Task<IActionResult> Download(int id)
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

                var invoice = await _context.Invoices
                    .Include(i => i.Contract)
                        .ThenInclude(c => c.Bid)
                            .ThenInclude(b => b.ShipmentRequest)
                    .Include(i => i.Contract.Provider)
                    .Include(i => i.Contract.Company)
                        .ThenInclude(c => c.User)
                    .Include(i => i.Payment)
                    .FirstOrDefaultAsync(i => i.InvoiceId == id &&
                        i.Contract.CompanyId == company.CompanyId);

                if (invoice == null)
                    return NotFound("Invoice not found");

                // Generate invoice content
                var content = GenerateInvoiceContent(invoice);
                var fileName = $"invoice-{invoice.InvoiceNumber}.txt";
                var bytes = System.Text.Encoding.UTF8.GetBytes(content);

                return File(bytes, "text/plain", fileName);
            }
            catch (Exception ex)
            {
                LogAndSetError(ex, "Error downloading invoice");
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Generate invoice content as text (placeholder for PDF)
        /// </summary>
        private string GenerateInvoiceContent(Invoice invoice)
        {
            var sb = new System.Text.StringBuilder();

            sb.AppendLine($"INVOICE {invoice.InvoiceNumber}");
            sb.AppendLine("========================\n");

            sb.AppendLine("Bill To:");
            sb.AppendLine("--------");
            sb.AppendLine($"Company: {invoice.Contract.Company.CompanyName}");
            sb.AppendLine($"Email: {invoice.Contract.Company.User.UserEmail}");
            sb.AppendLine($"Phone: {invoice.Contract.Company.PhoneNumber}\n");

            sb.AppendLine("Service Provider:");
            sb.AppendLine("-----------------");
            sb.AppendLine($"Name: {invoice.Contract.Provider.ProviderName}");
            sb.AppendLine($"Email: {invoice.Contract.Provider.ProviderEmail}\n");

            sb.AppendLine("Shipment Details:");
            sb.AppendLine("-----------------");
            sb.AppendLine($"Goods: {invoice.Contract.Bid.ShipmentRequest.GoodsType}");
            sb.AppendLine($"Weight: {invoice.Contract.Bid.ShipmentRequest.WeightKg} kg");
            sb.AppendLine($"Route: {invoice.Contract.Bid.ShipmentRequest.PickupCity} to {invoice.Contract.Bid.ShipmentRequest.DeliveryCity}\n");

            sb.AppendLine("Payment Details:");
            sb.AppendLine("----------------");
            sb.AppendLine($"Amount: SAR {invoice.Payment?.PaymentAmount:N2}");
            sb.AppendLine($"Status: {invoice.Payment?.PaymentStatus}");
            sb.AppendLine($"Due Date: {invoice.InvoiceDueDate:MMM dd, yyyy}");
            if (invoice.Payment?.PaymentDate.HasValue == true)
            {
                sb.AppendLine($"Paid Date: {invoice.Payment.PaymentDate:MMM dd, yyyy}");
            }

            return sb.ToString();
        }
    }
}
