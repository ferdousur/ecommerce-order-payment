using FluentValidation;

namespace ECommerce.Application.Features.Cart.Commands.AddToCart;

public class AddToCartCommandValidator : AbstractValidator<AddToCartCommand>
{
    public AddToCartCommandValidator()
    {
        RuleFor(x => x.UserProfileId)
            .NotEmpty()
            .WithMessage("UserProfileId is required.");

        RuleFor(x => x.ProductId)
            .NotEmpty()
            .WithMessage("ProductId is required.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than zero.")
            .LessThanOrEqualTo(100)
            .WithMessage("Cannot add more than 100 units of a product at once.");
    }
}