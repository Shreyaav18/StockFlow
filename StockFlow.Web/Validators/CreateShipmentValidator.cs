using FluentValidation;
using StockFlow.Web.DTOs.Shipment;

namespace StockFlow.Web.Validators
{
    public class CreateShipmentValidator : AbstractValidator<CreateShipmentDto>
    {
        public CreateShipmentValidator()
        {
            RuleFor(x => x.ItemId)
                .GreaterThan(0).WithMessage("A valid item must be selected.");

            RuleFor(x => x.TotalWeight)
                .GreaterThan(0).WithMessage("Shipment weight must be greater than zero.")
                .LessThanOrEqualTo(999999).WithMessage("Shipment weight exceeds the maximum allowed value.");
        }
    }
}