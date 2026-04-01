using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Serilog;
using StockFlow.Web.Data;
using StockFlow.Web.DTOs.Auth;
using StockFlow.Web.Exceptions;
using StockFlow.Web.Services.Interfaces;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace StockFlow.Web.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _db;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IAuditLogService _auditLogService;

        public AuthService(AppDbContext db, IHttpContextAccessor httpContextAccessor, IAuditLogService auditLogService)
        {
            _db = db;
            _httpContextAccessor = httpContextAccessor;
            _auditLogService = auditLogService;
        }

        public async Task<AuthResultViewModel> LoginAsync(LoginDto dto, CancellationToken ct = default)
        {
            try
            {
                var user = await _db.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Email == dto.Email.ToLower().Trim(), ct)
                    ?? throw new UnauthorizedException(ErrorMessages.Auth.InvalidCredentials);

                if (!VerifyPassword(dto.Password, user.PasswordHash))
                    throw new UnauthorizedException(ErrorMessages.Auth.InvalidCredentials);

                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                    new(ClaimTypes.Name, user.FullName),
                    new(ClaimTypes.Email, user.Email),
                    new(ClaimTypes.Role, user.Role)
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = dto.RememberMe,
                    ExpiresUtc = dto.RememberMe
                        ? DateTimeOffset.UtcNow.AddDays(7)
                        : DateTimeOffset.UtcNow.AddHours(8)
                };

                await _httpContextAccessor.HttpContext!
                    .SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);

                var ipAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "unknown";
                await _auditLogService.LogAsync(new DTOs.Report.AuditLogDto
                {
                    EntityName = "User",
                    EntityId = user.UserId,
                    Action = "Login",
                    PerformedBy = user.UserId,
                    Details = $"Login from {ipAddress}"
                }, ct);

                Log.Information("User {UserId} logged in successfully", user.UserId);

                return new AuthResultViewModel
                {
                    Success = true,
                    Message = "Login successful.",
                    User = new UserViewModel
                    {
                        UserId = user.UserId,
                        FullName = user.FullName,
                        Email = user.Email,
                        Role = user.Role,
                        CreatedAt = user.CreatedAt
                    }
                };
            }
            catch (AppException) { throw; }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error during login for {Email}", dto.Email);
                throw new AppException(ErrorMessages.General.ServerError);
            }
        }

        public async Task LogoutAsync(int userId, CancellationToken ct = default)
        {
            try
            {
                await _httpContextAccessor.HttpContext!
                    .SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                await _auditLogService.LogAsync(new DTOs.Report.AuditLogDto
                {
                    EntityName = "User",
                    EntityId = userId,
                    Action = "Logout",
                    PerformedBy = userId
                }, ct);

                Log.Information("User {UserId} logged out", userId);
            }
            catch (AppException) { throw; }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error during logout for user {UserId}", userId);
                throw new AppException(ErrorMessages.General.ServerError);
            }
        }

        public async Task<UserViewModel> RegisterAsync(RegisterDto dto, CancellationToken ct = default)
        {
            try
            {
                var exists = await _db.Users
                    .AnyAsync(u => u.Email == dto.Email.ToLower().Trim(), ct);

                if (exists)
                    throw new ConflictException(ErrorMessages.Auth.InvalidCredentials);

                if (!IsPasswordStrong(dto.Password))
                    throw new ValidationException(ErrorMessages.Auth.PasswordTooWeak);

                var user = new Models.User
                {
                    FullName = dto.FullName.Trim(),
                    Email = dto.Email.ToLower().Trim(),
                    PasswordHash = HashPassword(dto.Password),
                    Role = dto.Role,
                    CreatedAt = DateTime.UtcNow
                };

                _db.Users.Add(user);
                await _db.SaveChangesAsync(ct);

                await _auditLogService.LogAsync(new DTOs.Report.AuditLogDto
                {
                    EntityName = "User",
                    EntityId = user.UserId,
                    Action = "Register",
                    PerformedBy = user.UserId,
                    Details = $"Role: {user.Role}"
                }, ct);

                Log.Information("New user registered: {UserId} with role {Role}", user.UserId, user.Role);

                return new UserViewModel
                {
                    UserId = user.UserId,
                    FullName = user.FullName,
                    Email = user.Email,
                    Role = user.Role,
                    CreatedAt = user.CreatedAt
                };
            }
            catch (AppException) { throw; }
            catch (DbUpdateException ex)
            {
                Log.Error(ex, "Database error during registration for {Email}", dto.Email);
                throw new DatabaseException();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error during registration for {Email}", dto.Email);
                throw new AppException(ErrorMessages.General.ServerError);
            }
        }

        public async Task ChangePasswordAsync(int userId, ChangePasswordDto dto, CancellationToken ct = default)
        {
            try
            {
                var user = await _db.Users.FindAsync(new object[] { userId }, ct)
                    ?? throw new NotFoundException(ErrorMessages.Auth.AccountNotFound);

                if (!VerifyPassword(dto.CurrentPassword, user.PasswordHash))
                    throw new ValidationException(ErrorMessages.Auth.InvalidCredentials);

                if (!IsPasswordStrong(dto.NewPassword))
                    throw new ValidationException(ErrorMessages.Auth.PasswordTooWeak);

                user.PasswordHash = HashPassword(dto.NewPassword);
                await _db.SaveChangesAsync(ct);

                await _auditLogService.LogAsync(new DTOs.Report.AuditLogDto
                {
                    EntityName = "User",
                    EntityId = userId,
                    Action = "ChangePassword",
                    PerformedBy = userId
                }, ct);

                Log.Information("User {UserId} changed their password", userId);
            }
            catch (AppException) { throw; }
            catch (DbUpdateException ex)
            {
                Log.Error(ex, "Database error during password change for user {UserId}", userId);
                throw new DatabaseException();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error during password change for user {UserId}", userId);
                throw new AppException(ErrorMessages.General.ServerError);
            }
        }

        public async Task<UserViewModel> GetCurrentUserAsync(int userId, CancellationToken ct = default)
        {
            try
            {
                var user = await _db.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.UserId == userId, ct)
                    ?? throw new NotFoundException(ErrorMessages.Auth.AccountNotFound);

                return new UserViewModel
                {
                    UserId = user.UserId,
                    FullName = user.FullName,
                    Email = user.Email,
                    Role = user.Role,
                    CreatedAt = user.CreatedAt
                };
            }
            catch (AppException) { throw; }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error fetching current user {UserId}", userId);
                throw new AppException(ErrorMessages.General.ServerError);
            }
        }

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "StockFlow_Salt_2025"));
            return Convert.ToBase64String(bytes);
        }

        private static bool VerifyPassword(string password, string hash)
            => HashPassword(password) == hash;

        private static bool IsPasswordStrong(string password)
            => password.Length >= 8 &&
               password.Any(char.IsDigit) &&
               password.Any(c => !char.IsLetterOrDigit(c));
    }
}