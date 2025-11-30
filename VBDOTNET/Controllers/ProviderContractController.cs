using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wasl.Data;
using Wasl.Infrastructure;
using Wasl.Models;
using Wasl.ViewModels;
using Wasl.ViewModels.ProviderVMs;

namespace Wasl.Controllers
{
    [Authorize(Policy = "ProviderOnly")]
    public class ProviderContractController : BaseController
    {
        public ProviderContractController(
            WaslDbContext context,
            ILogger<ProviderContractController> logger,
            IFileUploadService fileUpload)
            : base(context, logger, fileUpload)
        {
        }

        // GET: /ProviderContract/Index
        public async Task<IActionResult> Index(string status = "all")
        {
            var userId = GetCurrentUserId();
            var provider = await _context.Providers
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (provider == null)
            {
                SetErrorMessage("Provider profile not found");
                return RedirectToAction("Index", "Home");
            }

            var query = _context.Contracts
                .Where(c => c.ProviderId == provider.ProviderId)
                .Include(c => c.Bid)
                    .ThenInclude(b => b.ShipmentRequest)
                .Include(c => c.Company)
                .OrderByDescending(c => c.SignDate);

            // Filter by status
            if (status != "all")
            {
                if (status == "signed")
                {
                    query = (IOrderedQueryable<Contract>)query.Where(c => c.SignDate != null);
                }
                else if (status == "pending")
                {
                    query = (IOrderedQueryable<Contract>)query.Where(c => c.SignDate == null);
                }
            }

            // Pagination
            int pageSize = 12;
            int pageNumber = 1;
            var totalItems = await query.CountAsync();
            var contracts = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var viewModel = new ProviderContractsViewModel
            {
                Contracts = contracts,
                StatusFilter = status,
                TotalItems = totalItems,
                PageSize = pageSize,
                CurrentPage = pageNumber
            };

            return View(viewModel);
        }

        // GET: /ProviderContract/ShowSign/5
        public async Task<IActionResult> ShowSign(int id)
        {
            var userId = GetCurrentUserId();
            var provider = await _context.Providers
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (provider == null)
            {
                return NotFound();
            }

            var contract = await _context.Contracts
                .Where(c => c.ContractId == id && c.ProviderId == provider.ProviderId)
                .Include(c => c.Bid)
                    .ThenInclude(b => b.ShipmentRequest)
                .Include(c => c.Company)
                .FirstOrDefaultAsync();

            if (contract == null)
            {
                return NotFound();
            }

            // Check if already signed
            if (contract.SignDate != null)
            {
                SetInfoMessage("This contract has already been signed");
                return RedirectToAction(nameof(Show), new { id });
            }

            var viewModel = new ContractSignViewModel
            {
                Contract = contract
            };

            return View(viewModel);
        }

        // POST: /ProviderContract/Sign/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Sign(int id, ContractSignViewModel model)
        {
            var userId = GetCurrentUserId();
            var provider = await _context.Providers
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (provider == null)
            {
                return NotFound();
            }

            var contract = await _context.Contracts
                .Where(c => c.ContractId == id && c.ProviderId == provider.ProviderId)
                .FirstOrDefaultAsync();

            if (contract == null)
            {
                return NotFound();
            }

            // Validate
            if (!model.TermsAccepted)
            {
                ModelState.AddModelError("TermsAccepted", "You must accept all terms and conditions to continue");
                return View("ShowSign", model);
            }

            if (model.SignedDocument == null || model.SignedDocument.Length == 0)
            {
                ModelState.AddModelError("SignedDocument", "Please upload the signed contract document");
                return View("ShowSign", model);
            }

            // Validate file type
            var allowedExtensions = new[] { ".pdf" };
            var extension = Path.GetExtension(model.SignedDocument.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                ModelState.AddModelError("SignedDocument", "Only PDF files are allowed");
                return View("ShowSign", model);
            }

            try
            {
                // Upload signed document
                string uploadPath = await _fileUploadService.UploadFileAsync(
                    model.SignedDocument,
                    "contracts",
                    $"contract-{contract.ContractId}-signed"
                );

                if (string.IsNullOrEmpty(uploadPath))
                {
                    SetErrorMessage("Failed to upload document. Please try again");
                    return View("ShowSign", model);
                }

                // Update contract
                contract.ContractDocument = uploadPath;
                contract.SignDate = DateTime.Now;

                await _context.SaveChangesAsync();

                SetSuccessMessage("Contract signed successfully!");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                LogAndSetError(ex, "Failed to sign contract. Please try again");
                return View("ShowSign", model);
            }
        }

        // GET: /ProviderContract/Show/5
        public async Task<IActionResult> Show(int id)
        {
            var userId = GetCurrentUserId();
            var provider = await _context.Providers
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (provider == null)
            {
                return NotFound();
            }

            var contract = await _context.Contracts
                .Where(c => c.ContractId == id && c.ProviderId == provider.ProviderId)
                .Include(c => c.Bid)
                    .ThenInclude(b => b.ShipmentRequest)
                .Include(c => c.Company)
                .FirstOrDefaultAsync();

            if (contract == null)
            {
                return NotFound();
            }

            // Get shipment for this contract
            var shipment = await _context.Shipments
                .Where(s => s.ContractId == contract.ContractId)
                .OrderByDescending(s => s.ActualStartDate)
                .FirstOrDefaultAsync();

            var viewModel = new ContractDetailsViewModel
            {
                Contract = contract,
                Shipment = shipment
            };

            return View(viewModel);
        }

        // GET: /ProviderContract/DownloadDocument/5
        public async Task<IActionResult> DownloadDocument(int id)
        {
            var userId = GetCurrentUserId();
            var provider = await _context.Providers
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (provider == null)
            {
                return NotFound();
            }

            var contract = await _context.Contracts
                .Where(c => c.ContractId == id && c.ProviderId == provider.ProviderId)
                .FirstOrDefaultAsync();

            if (contract == null || string.IsNullOrEmpty(contract.ContractDocument))
            {
                return NotFound();
            }

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", contract.ContractDocument.TrimStart('/'));

            if (!System.IO.File.Exists(filePath))
            {
                SetErrorMessage("Contract document not found");
                return RedirectToAction(nameof(Show), new { id });
            }

            var memory = new MemoryStream();
            using (var stream = new FileStream(filePath, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;

            var fileName = $"Contract-{contract.ContractId}.pdf";
            return File(memory, "application/pdf", fileName);
        }
    }
}