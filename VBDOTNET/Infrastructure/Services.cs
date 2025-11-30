using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Wasl.Data;
using Wasl.Models;

namespace Wasl.Infrastructure
{
    #region Status Helper
    /// <summary>
    /// Status helper methods for Bootstrap colors and icons
    /// </summary>
    public static class StatusHelper
    {
        public static string GetShipmentStatusColor(string status) => status switch
        {
            AppConstants.SHIPMENT_PENDING => "warning",
            AppConstants.SHIPMENT_BIDDING => "info",
            AppConstants.SHIPMENT_ASSIGNED => "primary",
            AppConstants.SHIPMENT_ACCEPTED => "success",
            AppConstants.SHIPMENT_IN_PROGRESS => "primary",
            AppConstants.SHIPMENT_DELIVERED => "success",
            AppConstants.SHIPMENT_CANCELLED => "danger",
            AppConstants.SHIPMENT_FAILED => "dark",
            _ => "secondary"
        };

        public static string GetBidStatusColor(string status) => status switch
        {
            AppConstants.BID_SUBMITTED => "info",
            AppConstants.BID_UNDER_REVIEW => "warning",
            AppConstants.BID_ACCEPTED => "success",
            AppConstants.BID_REJECTED => "danger",
            AppConstants.BID_CANCELLED => "secondary",
            _ => "secondary"
        };

        public static string GetPaymentStatusColor(string status) => status switch
        {
            AppConstants.PAYMENT_SUCCESSFUL => "success",
            AppConstants.PAYMENT_PENDING => "warning",
            AppConstants.PAYMENT_PROCESSING => "info",
            AppConstants.PAYMENT_FAILED => "danger",
            AppConstants.PAYMENT_REFUNDED => "secondary",
            _ => "secondary"
        };

        public static string GetPaymentStatusIcon(string status) => status switch
        {
            AppConstants.PAYMENT_SUCCESSFUL => "check-circle",
            AppConstants.PAYMENT_PENDING => "clock",
            AppConstants.PAYMENT_PROCESSING => "arrow-repeat",
            AppConstants.PAYMENT_FAILED => "x-circle",
            AppConstants.PAYMENT_REFUNDED => "arrow-counterclockwise",
            _ => "question-circle"
        };
    }
    #endregion

    #region File Upload Service
    /// <summary>
    /// File upload service interface
    /// </summary>
    public interface IFileUploadService
    {
        Task<string> UploadFileAsync(IFormFile file, string path = "uploads", string slug = "file");
        Task<bool> DeleteFileAsync(string filePath);
    }

    /// <summary>
    /// File upload service implementation
    /// </summary>
    public class FileUploadService : IFileUploadService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<FileUploadService> _logger;

        public FileUploadService(IWebHostEnvironment environment, ILogger<FileUploadService> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        public async Task<string> UploadFileAsync(IFormFile file, string path = "uploads", string slug = "file")
        {
            try
            {
                if (file == null || file.Length == 0) return null;

                string slugified = Slugify(slug);
                string currentDate = DateTime.Now.ToString("yyyy-MM-dd");
                string extension = Path.GetExtension(file.FileName);
                string uniqueId = Guid.NewGuid().ToString("N")[..8];
                string fileName = $"{slugified}-{currentDate}-{uniqueId}{extension}";

                string uploadPath = Path.Combine(_environment.WebRootPath, path);
                if (!Directory.Exists(uploadPath))
                    Directory.CreateDirectory(uploadPath);

                string fullPath = Path.Combine(uploadPath, fileName);
                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                return $"/{path}/{fileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file");
                return null;
            }
        }

        public async Task<bool> DeleteFileAsync(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath)) return false;

                string fullPath = Path.Combine(_environment.WebRootPath, filePath.TrimStart('/'));
                if (File.Exists(fullPath))
                {
                    await Task.Run(() => File.Delete(fullPath));
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file");
                return false;
            }
        }

        private static string Slugify(string text)
        {
            if (string.IsNullOrEmpty(text)) return "file";
            text = text.ToLowerInvariant();
            text = Regex.Replace(text, @"[^a-z0-9\s-]", "");
            text = Regex.Replace(text, @"\s+", "-");
            text = Regex.Replace(text, @"-+", "-");
            return text.Trim('-');
        }
    }
    #endregion

    #region Logger Extensions
    /// <summary>
    /// Extension methods for logging
    /// </summary>
    public static class LoggerExtensions
    {
        public static void LogError(this ILogger logger, Exception ex, string msg = null)
        {
            logger.LogError(ex, "Error: {Message} | Stack: {Stack} | Additional: {Additional}",
                ex.Message, ex.StackTrace, msg ?? "N/A");
        }
    }
    #endregion

    #region Authorization
    /// <summary>
    /// Custom authorization attribute for role checking
    /// </summary>
    public class RoleAuthorizeAttribute : AuthorizeAttribute
    {
        public RoleAuthorizeAttribute(params string[] roles)
        {
            Roles = string.Join(",", roles);
        }
    }

    /// <summary>
    /// Extension methods for authorization policies
    /// </summary>
    public static class AuthorizationExtensions
    {
        public static void AddWaslPolicies(this AuthorizationOptions options)
        {
            options.AddPolicy("AdminOnly", p => p.RequireRole(AppConstants.ROLE_ADMIN));
            options.AddPolicy("CompanyOnly", p => p.RequireRole(AppConstants.ROLE_COMPANY));
            options.AddPolicy("ProviderOnly", p => p.RequireRole(AppConstants.ROLE_PROVIDER));
            options.AddPolicy("CompanyOrProvider", p =>
                p.RequireRole(AppConstants.ROLE_COMPANY, AppConstants.ROLE_PROVIDER));
            options.AddPolicy("AdminOrCompany", p =>
                p.RequireRole(AppConstants.ROLE_ADMIN, AppConstants.ROLE_COMPANY));
            options.AddPolicy("AdminOrProvider", p =>
                p.RequireRole(AppConstants.ROLE_ADMIN, AppConstants.ROLE_PROVIDER));
            options.AddPolicy("Authenticated", p => p.RequireAuthenticatedUser());
        }
    }
    #endregion

    #region View Components
    /// <summary>
    /// Provider Sidebar View Component
    /// </summary>
    public class ProviderSidebarViewComponent : ViewComponent
    {
        private readonly WaslDbContext _context;

        public ProviderSidebarViewComponent(WaslDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var userIdClaim = UserClaimsPrincipal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                return View(new ProviderSidebarViewModel());

            var provider = await _context.Providers
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (provider == null)
                return View(new ProviderSidebarViewModel());

            var bids = await _context.Bids
                .Where(b => b.ProviderId == provider.ProviderId)
                .ToListAsync();

            var contracts = await _context.Contracts
                .Where(c => c.ProviderId == provider.ProviderId)
                .ToListAsync();

            var feedbacks = await _context.Feedbacks
                .Where(f => f.ProviderId == provider.ProviderId)
                .ToListAsync();

            var viewModel = new ProviderSidebarViewModel
            {
                Provider = provider,
                Stats = new ProviderStats
                {
                    TotalBids = bids.Count,
                    ActiveBids = bids.Count(b => b.BidStatus == AppConstants.BID_SUBMITTED),
                    WonContracts = contracts.Count,
                    AverageRating = feedbacks.Any() ? feedbacks.Average(f => f.Rating ?? 0) : 0,
                    TotalReviews = feedbacks.Count,
                    DirectRequests = bids.Count(b => b.BidStatus == AppConstants.BID_UNDER_REVIEW)
                }
            };

            return View(viewModel);
        }
    }

    /// <summary>
    /// View Model for Provider Sidebar
    /// </summary>
    public class ProviderSidebarViewModel
    {
        public Provider Provider { get; set; }
        public ProviderStats Stats { get; set; }
    }

    /// <summary>
    /// Provider Statistics
    /// </summary>
    public class ProviderStats
    {
        public int TotalBids { get; set; }
        public int ActiveBids { get; set; }
        public int WonContracts { get; set; }
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public int DirectRequests { get; set; }
    }
    #endregion
}