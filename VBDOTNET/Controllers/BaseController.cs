
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;
using Wasl.Data;
using Wasl.Infrastructure;

namespace Wasl.Controllers
{
    /// <summary>
    /// Base controller with common functionality
    /// </summary>
    public class BaseController : Controller
    {
        protected readonly WaslDbContext _context;
        protected readonly ILogger _logger;
        protected readonly IFileUploadService _fileUploadService;

        public BaseController(
            WaslDbContext context,
            ILogger logger,
            IFileUploadService fileUploadService = null)
        {
            _context = context;
            _logger = logger;
            _fileUploadService = fileUploadService;
        }

        /// <summary>
        /// Get current user ID from claims
        /// </summary>
        protected int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out int userId) ? userId : null;
        }

        /// <summary>
        /// Get current user role
        /// </summary>
        protected string GetCurrentUserRole()
            => User.FindFirst(ClaimTypes.Role)?.Value;

        /// <summary>
        /// Get current user email
        /// </summary>
        protected string GetCurrentUserEmail()
            => User.FindFirst(ClaimTypes.Email)?.Value;

        /// <summary>
        /// Check if user is authenticated
        /// </summary>
        protected bool IsAuthenticated()
            => User.Identity?.IsAuthenticated ?? false;

        /// <summary>
        /// Set success message in TempData
        /// </summary>
        protected void SetSuccessMessage(string message)
            => TempData["SuccessMessage"] = message;

        /// <summary>
        /// Set error message in TempData
        /// </summary>
        protected void SetErrorMessage(string message)
            => TempData["ErrorMessage"] = message;

        /// <summary>
        /// Set warning message in TempData
        /// </summary>
        protected void SetWarningMessage(string message)
            => TempData["WarningMessage"] = message;

        /// <summary>
        /// Set info message in TempData
        /// </summary>
        protected void SetInfoMessage(string message)
            => TempData["InfoMessage"] = message;

        /// <summary>
        /// Log error and set error message
        /// </summary>
        protected void LogAndSetError(Exception exception, string userMessage = "An error occurred")
        {
            _logger.LogError(exception, userMessage);
            SetErrorMessage(userMessage);
        }

        /// <summary>
        /// Override OnActionExecuting to add common functionality
        /// </summary>
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (IsAuthenticated())
            {
                ViewBag.CurrentUserId = GetCurrentUserId();
                ViewBag.CurrentUserRole = GetCurrentUserRole();
                ViewBag.CurrentUserEmail = GetCurrentUserEmail();
            }
            base.OnActionExecuting(context);
        }

        /// <summary>
        /// Return JSON error response
        /// </summary>
        protected JsonResult JsonError(string message, int statusCode = 400)
        {
            Response.StatusCode = statusCode;
            return Json(new { success = false, message });
        }

        /// <summary>
        /// Return JSON success response
        /// </summary>
        protected JsonResult JsonSuccess(string message, object data = null)
            => Json(new { success = true, message, data });
    }
}