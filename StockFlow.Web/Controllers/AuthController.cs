using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using StockFlow.Web.DTOs.Auth;
using StockFlow.Web.Exceptions;
using StockFlow.Web.Services.Interfaces;

namespace StockFlow.Web.Controllers
{
    public class AuthController : BaseController
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Dashboard");

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("LoginPolicy")]
        public async Task<IActionResult> Login(LoginDto dto, string? returnUrl = null, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
                return View(dto);

            try
            {
                var result = await _authService.LoginAsync(dto, ct);

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);

                return RedirectToAction("Index", "Dashboard");
            }
            catch (UnauthorizedException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(dto);
            }
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout(CancellationToken ct = default)
        {
            await _authService.LogoutAsync(CurrentUserId, ct);
            return RedirectToAction("Login");
        }

        [HttpGet]
        [Authorize(Policy = "AdminOnly")]
        public IActionResult Register()
            => View();

        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterDto dto, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
                return View(dto);

            try
            {
                await _authService.RegisterAsync(dto, ct);
                SetSuccessMessage($"User {dto.Email} registered successfully.");
                return RedirectToAction("Users", "Admin");
            }
            catch (ConflictException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(dto);
            }
            catch (ValidationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(dto);
            }
        }

        [HttpGet]
        [Authorize]
        public IActionResult ChangePassword()
            => View();

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordDto dto, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
                return View(dto);

            try
            {
                await _authService.ChangePasswordAsync(CurrentUserId, dto, ct);
                SetSuccessMessage("Password changed successfully.");
                return RedirectToAction("Index", "Dashboard");
            }
            catch (ValidationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(dto);
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
            => View();
    }
}