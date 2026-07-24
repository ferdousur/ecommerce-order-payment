using FluentValidation;

namespace ECommerce.Application.Features.Category.Command.CreateCategory;

public class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Category name is required.")
            .MaximumLength(100);

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .When(x => x.Description is not null);

        RuleFor(x => x.ParentCategoryId)
            .NotEqual(Guid.Empty).WithMessage("ParentCategoryId cannot be empty.")
            .When(x => x.ParentCategoryId is not null);
    }
}