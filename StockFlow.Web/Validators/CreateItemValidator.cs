using FluentValidation;
using StockFlow.Web.DTOs.Item;

namespace StockFlow.Web.Validators
{
    public class CreateItemValidator : AbstractValidator<CreateItemDto>
    {
        private static readonly string[] AllowedUnits = { "kg", "g", "lb", "oz", "ton", "unit", "pcs", "box", "pallet" };

        public CreateItemValidator()
        {
            RuleFor(x => x.ItemName)
                .NotEmpty().WithMessage("Item name is required.")
                .MaximumLength(150).WithMessage("Item name must not exceed 150 characters.")
                .Matches(@"^[a-zA-Z0-9\s\-_]+$").WithMessage("Item name contains invalid characters.");

            RuleFor(x => x.SKU)
                .NotEmpty().WithMessage("SKU is required.")
                .MaximumLength(50).WithMessage("SKU must not exceed 50 characters.")
                .Matches(@"^[A-Z0-9\-_]+$").WithMessage("SKU must be uppercase alphanumeric with hyphens or underscores only.");

            RuleFor(x => x.Unit)
                .NotEmpty().WithMessage("Unit is required.")
                .Must(u => AllowedUnits.Contains(u.ToLower()))
                .WithMessage($"Unit must be one of: {string.Join(", ", AllowedUnits)}.");
        }
    }

    public class UpdateItemValidator : AbstractValidator<UpdateItemDto>
    {
        private static readonly string[] AllowedUnits = { "kg", "g", "lb", "oz", "ton", "unit", "pcs", "box", "pallet" };

        public UpdateItemValidator()
        {
            RuleFor(x => x.ItemName)
                .NotEmpty().WithMessage("Item name is required.")
                .MaximumLength(150).WithMessage("Item name must not exceed 150 characters.")
                .Matches(@"^[a-zA-Z0-9\s\-_]+$").WithMessage("Item name contains invalid characters.");

            RuleFor(x => x.Unit)
                .NotEmpty().WithMessage("Unit is required.")
                .Must(u => AllowedUnits.Contains(u.ToLower()))
                .WithMessage($"Unit must be one of: {string.Join(", ", AllowedUnits)}.");
        }
    }
}