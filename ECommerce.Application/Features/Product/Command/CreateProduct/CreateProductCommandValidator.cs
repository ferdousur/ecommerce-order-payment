using FluentValidation;

namespace ECommerce.Application.Features.Product.Command.CreateProduct;

public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Product name is required.")
            .MaximumLength(150).WithMessage("Product name must not exceed 150 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than 0.");

        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("Stock quantity cannot be negative.");

        // একক CategoryId এর বদলে CategoryIds (List<Guid>) ভ্যালিডেশন
        RuleFor(x => x.CategoryIds)
            .NotEmpty().WithMessage("At least one category is required.")
            .Must(ids => ids != null && ids.Any()).WithMessage("Category list cannot be empty.");

        // লিস্টের ভেতরের প্রতিটা Guid যেন Empty না হয়
        RuleForEach(x => x.CategoryIds)
            .NotEmpty().WithMessage("Category ID cannot be empty.");

        RuleFor(x => x.SKU)
            .MaximumLength(50).WithMessage("SKU must not exceed 50 characters.")
            .When(x => !string.IsNullOrEmpty(x.SKU));
    }
}