using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Wasl.Data;
using Wasl.Infrastructure;
using Wasl.Models;
using Wasl.ViewModels;
using Wasl.ViewModels.Auth;

namespace Wasl.Controllers
{
    public class AuthController : BaseController
    {
        public AuthController(
            WaslDbContext context,
            ILogger<AuthController> logger,
            IFileUploadService fileUpload)
            : base(context, logger, fileUpload)
        {
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserEmail == model.Email);

                if (user == null)
                {
                    ModelState.AddModelError("", "Invalid email or password");
                    return View(model);
                }

                if (!BCrypt.Net.BCrypt.Verify(model.Password, user.UserPassword))
                {
                    ModelState.AddModelError("", "Invalid email or password");
                    return View(model);
                }

                if (user.UserRole == AppConstants.ROLE_COMPANY)
                {
                    var company = await _context.Companies.FirstOrDefaultAsync(c => c.UserId == user.UserId);
                    if (company == null || company.IsApproved != true)
                    {
                        ModelState.AddModelError("", "Your account is pending approval");
                        return View(model);
                    }
                }
                else if (user.UserRole == AppConstants.ROLE_PROVIDER)
                {
                    var provider = await _context.Providers.FirstOrDefaultAsync(p => p.UserId == user.UserId);
                    if (provider == null || provider.IsApproved != true)
                    {
                        ModelState.AddModelError("", "Your account is pending approval");
                        return View(model);
                    }
                }

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                    new Claim(ClaimTypes.Name, user.UserName),
                    new Claim(ClaimTypes.Email, user.UserEmail),
                    new Claim(ClaimTypes.Role, user.UserRole ?? AppConstants.ROLE_COMPANY)
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(24)
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                SetSuccessMessage($"Welcome back, {user.UserName}!");

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);

                return user.UserRole switch
                {
                    AppConstants.ROLE_ADMIN => RedirectToAction("Dashboard", "Admin"),
                    AppConstants.ROLE_COMPANY => RedirectToAction("Dashboard", "Company"),
                    AppConstants.ROLE_PROVIDER => RedirectToAction("Dashboard", "Provider"),
                    _ => RedirectToAction("Index", "Home")
                };
            }
            catch (Exception ex)
            {
                LogAndSetError(ex, "An error occurred during login");
                return View(model);
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult RegisterCompany()
        {
            ViewBag.Regions = KSALocations.Regions;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterCompany(CompanyRegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Regions = KSALocations.Regions;
                return View(model);
            }

            if (await _context.Users.AnyAsync(u => u.UserEmail == model.Email))
            {
                ModelState.AddModelError("Email", "This email is already registered");
                ViewBag.Regions = KSALocations.Regions;
                return View(model);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var user = new User
                {
                    UserName = model.CompanyName,
                    UserEmail = model.Email,
                    UserPassword = BCrypt.Net.BCrypt.HashPassword(model.Password),
                    UserRole = AppConstants.ROLE_COMPANY,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                var company = new Wasl.Models.Company
                {
                    UserId = user.UserId,
                    CompanyName = model.CompanyName,
                    BusinessRegistrationNumber = model.BusinessRegistrationNumber,
                    CompanyAddress = model.CompanyAddress,
                    CompanyCity = model.CompanyCity,
                    CompanyRegion = model.CompanyRegion,
                    CompanyEmail = model.CompanyEmail,
                    PhoneNumber = model.PhoneNumber,
                    IsApproved = false,
                    CompanyStatus = AppConstants.COMPANY_PENDING_APPROVAL
                };

                _context.Companies.Add(company);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                SetSuccessMessage("Registration successful! Your account is pending admin approval.");
                return RedirectToAction(nameof(Login));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                LogAndSetError(ex, "Registration failed. Please try again");
                ViewBag.Regions = KSALocations.Regions;
                return View(model);
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult RegisterProvider()
        {
            ViewBag.Regions = KSALocations.Regions;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterProvider(ProviderRegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Regions = KSALocations.Regions;
                return View(model);
            }

            if (await _context.Users.AnyAsync(u => u.UserEmail == model.Email))
            {
                ModelState.AddModelError("Email", "This email is already registered");
                ViewBag.Regions = KSALocations.Regions;
                return View(model);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var user = new User
                {
                    UserName = model.ProviderName,
                    UserEmail = model.Email,
                    UserPassword = BCrypt.Net.BCrypt.HashPassword(model.Password),
                    UserRole = AppConstants.ROLE_PROVIDER,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                var provider = new Provider
                {
                    UserId = user.UserId,
                    ProviderName = model.ProviderName,
                    BusinessRegistrationNumber = model.BusinessRegistrationNumber,
                    ProviderAddress = model.ProviderAddress,
                    ProviderCity = model.ProviderCity,
                    ProviderRegion = model.ProviderRegion,
                    ServiceDescription = model.ServiceDescription,
                    ProviderEmail = model.ProviderEmail,
                    ProviderPhoneNumber = model.ProviderPhoneNumber,
                    IsApproved = false
                };

                _context.Providers.Add(provider);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                SetSuccessMessage("Registration successful! Your account is pending admin approval.");
                return RedirectToAction(nameof(Login));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                LogAndSetError(ex, "Registration failed. Please try again");
                ViewBag.Regions = KSALocations.Regions;
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            SetInfoMessage("You have been logged out successfully");
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpGet]
        public IActionResult GetCities(string region)
        {
            if (string.IsNullOrEmpty(region) || !KSALocations.CitiesByRegion.ContainsKey(region))
                return Json(new Dictionary<string, string>());

            return Json(KSALocations.CitiesByRegion[region]);
        }


        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                SetErrorMessage("Please enter your email address");
                return View();
            }

            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserEmail == email);

                if (user == null)
                {
                    // Don't reveal that the user doesn't exist for security
                    TempData["status"] = "If an account exists with that email, you will receive a password reset link shortly.";
                    return View();
                }

                // TODO: Implement email sending logic here
                // For now, just show a success message
                TempData["status"] = "Password reset instructions have been sent to your email.";
                return View();
            }
            catch (Exception ex)
            {
                LogAndSetError(ex, "An error occurred while processing your request");
                return View();
            }
        }
    }
}