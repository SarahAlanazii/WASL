using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wasl.Data;
using Wasl.Infrastructure;
using Wasl.Models;
using Wasl.ViewModels.CompanyVMs;

namespace Wasl.Controllers.Company
{
    /// <summary>
    /// Company Payment Controller - Handles payment processing
    /// </summary>
    [RoleAuthorize(AppConstants.ROLE_COMPANY)]
    [Route("Company/Payments")]
    public class CompanyPaymentController : BaseController
    {
        public CompanyPaymentController(
            WaslDbContext context,
            ILogger<CompanyPaymentController> logger,
            IFileUploadService fileUploadService)
            : base(context, logger, fileUploadService)
        {
        }

        /// <summary>
        /// Display all payments for company
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

                var query = _context.Payments
                    .Include(p => p.Invoice)
                        .ThenInclude(i => i.Contract)
                            .ThenInclude(c => c.Bid)
                                .ThenInclude(b => b.ShipmentRequest)
                    .Include(p => p.Invoice.Contract.Provider)
                    .Where(p => p.Invoice.Contract.CompanyId == company.CompanyId);

                // Filter by status
                if (status != "all")
                {
                    query = query.Where(p => p.PaymentStatus == status);
                }

                var payments = await query
                    .OrderByDescending(p => p.PaymentDate)
                    .ToListAsync();

                ViewBag.StatusFilter = status;
                return View(payments);
            }
            catch (Exception ex)
            {
                LogAndSetError(ex, "Error loading payments");
                return View(new List<Payment>());
            }
        }

        /// <summary>
        /// Show payment details
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

                var payment = await _context.Payments
                    .Include(p => p.Invoice)
                        .ThenInclude(i => i.Contract)
                            .ThenInclude(c => c.Bid)
                                .ThenInclude(b => b.ShipmentRequest)
                    .Include(p => p.Invoice.Contract.Provider)
                    .FirstOrDefaultAsync(p => p.PaymentId == id &&
                        p.Invoice.Contract.CompanyId == company.CompanyId);

                if (payment == null)
                    return NotFound("Payment not found");

                return View(payment);
            }
            catch (Exception ex)
            {
                LogAndSetError(ex, "Error loading payment details");
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Show payment processing form
        /// </summary>
        [HttpGet("Process/{id}")]
        public async Task<IActionResult> ProcessForm(int id)
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

                var payment = await _context.Payments
                    .Include(p => p.Invoice)
                        .ThenInclude(i => i.Contract)
                    .FirstOrDefaultAsync(p => p.PaymentId == id &&
                        p.PaymentStatus == AppConstants.PAYMENT_PENDING &&
                        p.Invoice.Contract.CompanyId == company.CompanyId);

                if (payment == null)
                    return NotFound("Payment not found or already processed");

                ViewBag.Payment = payment;
                return View(new PaymentViewModel());
            }
            catch (Exception ex)
            {
                LogAndSetError(ex, "Error loading payment form");
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Process payment (simulate payment gateway)
        /// </summary>
        [HttpPost("Process/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Process(int id, PaymentViewModel model)
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

                var payment = await _context.Payments
                    .Include(p => p.Invoice)
                        .ThenInclude(i => i.Contract)
                            .ThenInclude(c => c.Bid)
                                .ThenInclude(b => b.ShipmentRequest)
                    .FirstOrDefaultAsync(p => p.PaymentId == id &&
                        p.PaymentStatus == AppConstants.PAYMENT_PENDING &&
                        p.Invoice.Contract.CompanyId == company.CompanyId);

                if (payment == null)
                    return NotFound("Payment not found or already processed");

                if (!ModelState.IsValid)
                {
                    ViewBag.Payment = payment;
                    return View("ProcessForm", model);
                }

                // Simulate payment processing
                var success = SimulatePaymentProcessing(model);

                if (success)
                {
                    payment.PaymentStatus = AppConstants.PAYMENT_SUCCESSFUL;
                    payment.PaymentDate = DateTime.Now;
                    payment.PaymentMethod = "credit_card";
                    payment.TransactionId = Guid.NewGuid().ToString("N").Substring(0, 16).ToUpper();

                    // Update shipment request status
                    if (payment.Invoice.Contract.Bid?.ShipmentRequest != null)
                    {
                        payment.Invoice.Contract.Bid.ShipmentRequest.Status = AppConstants.SHIPMENT_IN_PROGRESS;
                        payment.Invoice.Contract.Bid.ShipmentRequest.UpdateAt = DateTime.Now;
                    }

                    await _context.SaveChangesAsync();

                    SetSuccessMessage("Payment processed successfully! Your shipment is now in progress.");
                    return RedirectToAction(nameof(Show), new { id = payment.PaymentId });
                }
                else
                {
                    payment.PaymentStatus = AppConstants.PAYMENT_FAILED;
                    await _context.SaveChangesAsync();

                    SetErrorMessage("Payment failed. Please check your card details and try again.");
                    ViewBag.Payment = payment;
                    return View("ProcessForm", model);
                }
            }
            catch (Exception ex)
            {
                LogAndSetError(ex, "Payment processing error. Please try again.");
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Retry failed payment
        /// </summary>
        [HttpPost("Retry/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Retry(int id)
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

                var payment = await _context.Payments
                    .Include(p => p.Invoice)
                        .ThenInclude(i => i.Contract)
                    .FirstOrDefaultAsync(p => p.PaymentId == id &&
                        p.PaymentStatus == AppConstants.PAYMENT_FAILED &&
                        p.Invoice.Contract.CompanyId == company.CompanyId);

                if (payment == null)
                    return NotFound("Payment not found");

                payment.PaymentStatus = AppConstants.PAYMENT_PENDING;
                await _context.SaveChangesAsync();

                SetSuccessMessage("Payment status reset to pending. You can now retry the payment.");
                return RedirectToAction(nameof(Show), new { id = payment.PaymentId });
            }
            catch (Exception ex)
            {
                LogAndSetError(ex, "Error retrying payment");
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Simulate payment processing (replace with real payment gateway)
        /// 90% success rate for testing
        /// </summary>
        private bool SimulatePaymentProcessing(PaymentViewModel cardData)
        {
            // Simulate processing delay
            System.Threading.Thread.Sleep(2000);

            // 90% success rate
            var random = new Random();
            return random.Next(1, 11) <= 9;
        }
    }
}
