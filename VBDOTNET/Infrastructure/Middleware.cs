// ============================================================
// File: Infrastructure/Middleware.cs
// Purpose: All middleware in one place
// ============================================================

using Microsoft.AspNetCore.Authentication;
using System.Net;
using System.Security.Claims;
using System.Text.Json;

namespace Wasl.Infrastructure
{
    #region Error Handling Middleware
    /// <summary>
    /// Global error handling middleware
    /// </summary>
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;

        public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception: {Message} | Path: {Path}",
                ex.Message, context.Request.Path);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var env = context.RequestServices.GetService<IWebHostEnvironment>();
            var response = new
            {
                StatusCode = context.Response.StatusCode,
                Message = "An error occurred processing your request.",
                Details = env?.IsDevelopment() == true ? ex.Message : null
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
    #endregion

    #region Redirect If Authenticated Middleware
    /// <summary>
    /// Redirect authenticated users away from login/register pages
    /// </summary>
    public class RedirectIfAuthenticatedMiddleware
    {
        private readonly RequestDelegate _next;

        public RedirectIfAuthenticatedMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var path = context.Request.Path.Value?.ToLower();

                if (path == "/auth/login" || path == "/auth/registercompany" ||
                    path == "/auth/registerprovider")
                {
                    var role = context.User.FindFirst(ClaimTypes.Role)?.Value;
                    string redirectUrl = role switch
                    {
                        AppConstants.ROLE_ADMIN => "/Admin/Dashboard",
                        AppConstants.ROLE_COMPANY => "/Company/Dashboard",
                        AppConstants.ROLE_PROVIDER => "/Provider/Dashboard",
                        _ => "/Home/Index"
                    };
                    context.Response.Redirect(redirectUrl);
                    return;
                }
            }
            await _next(context);
        }
    }
    #endregion

    #region Session Timeout Middleware
    /// <summary>
    /// Session timeout middleware - logs out users after inactivity
    /// </summary>
    public class SessionTimeoutMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly int _timeoutMinutes;

        public SessionTimeoutMiddleware(RequestDelegate next, int timeoutMinutes = 120)
        {
            _next = next;
            _timeoutMinutes = timeoutMinutes;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var lastActivity = context.Session.GetString("LastActivityTime");

                if (!string.IsNullOrEmpty(lastActivity))
                {
                    var lastActivityTime = long.Parse(lastActivity);
                    var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    var elapsedMinutes = (currentTime - lastActivityTime) / 60;

                    if (elapsedMinutes > _timeoutMinutes)
                    {
                        await context.SignOutAsync("WaslAuth");
                        context.Session.Clear();
                        context.Response.Redirect("/Auth/Login?timeout=true");
                        return;
                    }
                }

                context.Session.SetString("LastActivityTime",
                    DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
            }
            await _next(context);
        }
    }
    #endregion

    #region Middleware Extensions
    /// <summary>
    /// Extension methods for middleware registration
    /// </summary>
    public static class MiddlewareExtensions
    {
        public static IApplicationBuilder UseErrorHandling(this IApplicationBuilder builder)
            => builder.UseMiddleware<ErrorHandlingMiddleware>();

        public static IApplicationBuilder UseRedirectIfAuthenticated(this IApplicationBuilder builder)
            => builder.UseMiddleware<RedirectIfAuthenticatedMiddleware>();

        public static IApplicationBuilder UseSessionTimeout(this IApplicationBuilder builder, int timeoutMinutes = 120)
            => builder.UseMiddleware<SessionTimeoutMiddleware>(timeoutMinutes);
    }
    #endregion
}