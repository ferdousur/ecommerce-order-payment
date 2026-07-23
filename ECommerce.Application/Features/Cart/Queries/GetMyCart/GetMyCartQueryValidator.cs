using FluentValidation;

namespace ECommerce.Application.Features.Cart.Queries.GetMyCart;

public class GetMyCartQueryValidator : AbstractValidator<GetMyCartQuery>
{
    public GetMyCartQueryValidator()
    {
        RuleFor(x => x.UserProfileId)
            .NotEmpty()
            .WithMessage("UserProfileId is required and must be a valid GUID.");
    }
}