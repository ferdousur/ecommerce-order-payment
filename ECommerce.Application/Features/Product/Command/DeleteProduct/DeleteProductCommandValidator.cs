using FluentValidation;

namespace ECommerce.Application.Features.Product.Command.DeleteProduct;

public class DeleteProductCommandValidator : AbstractValidator<DeleteProductCommand>
{
    public DeleteProductCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Product Id is required.")
            .Must(id => id != Guid.Empty).WithMessage("A valid Product Id must be provided.");
    }
}