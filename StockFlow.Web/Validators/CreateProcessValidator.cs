using FluentValidation;
using StockFlow.Web.DTOs.Process;

namespace StockFlow.Web.Validators
{
    public class CreateProcessValidator : AbstractValidator<CreateProcessDto>
    {
        public CreateProcessValidator()
        {
            RuleFor(x => x.ShipmentId)
                .GreaterThan(0).WithMessage("A valid shipment must be selected.");

            RuleFor(x => x.Children)
                .NotEmpty().WithMessage("At least one child item is required.")
                .Must(c => c.Count <= 50).WithMessage("A maximum of 50 child items can be processed at once.");

            RuleForEach(x => x.Children).SetValidator(new ChildItemValidator());
        }
    }

    public class ChildItemValidator : AbstractValidator<ChildItemDto>
    {
        public ChildItemValidator()
        {
            RuleFor(x => x.ItemId)
                .GreaterThan(0).WithMessage("A valid item must be selected for each child.");

            RuleFor(x => x.OutputWeight)
                .GreaterThan(0).WithMessage("Output weight must be greater than zero.")
                .LessThanOrEqualTo(999999).WithMessage("Output weight exceeds the maximum allowed value.");
        }
    }
}