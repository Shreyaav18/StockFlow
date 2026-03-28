using FluentValidation;
using StockFlow.Web.DTOs.Auth;

namespace StockFlow.Web.Validators
{
    public class RegisterValidator : AbstractValidator<RegisterDto>
    {
        private static readonly string[] AllowedRoles = { "Admin", "Manager", "Staff" };

        public RegisterValidator()
        {
            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage("Full name is required.")
                .MaximumLength(100).WithMessage("Full name must not exceed 100 characters.")
                .Matches(@"^[a-zA-Z\s]+$").WithMessage("Full name can only contain letters and spaces.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("A valid email address is required.")
                .MaximumLength(150).WithMessage("Email must not exceed 150 characters.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.")
                .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
                .Matches(@"[0-9]").WithMessage("Password must contain at least one number.")
                .Matches(@"[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");

            RuleFor(x => x.ConfirmPassword)
                .NotEmpty().WithMessage("Please confirm your password.")
                .Equal(x => x.Password).WithMessage("Passwords do not match.");

            RuleFor(x => x.Role)
                .NotEmpty().WithMessage("Role is required.")
                .Must(r => AllowedRoles.Contains(r)).WithMessage("Invalid role specified.");
        }
    }
}