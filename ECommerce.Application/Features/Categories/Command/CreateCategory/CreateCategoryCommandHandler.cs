using ECommerce.Application.Categories.DTOs;
using ECommerce.Application.Cores.Abstractions;
using ECommerce.Application.Interfaces;
using ErrorOr;

namespace ECommerce.Application.Features.Category.Command.CreateCategory;

public class CreateCategoryCommandHandler : ICommandHandler<CreateCategoryCommand, ErrorOr<CategoryDto>>
{
    private readonly IRepository<Domain.Entities.Category> _repository;

    public CreateCategoryCommandHandler(IRepository<Domain.Entities.Category> repository)
    {
        _repository = repository;
    }

    public async Task<ErrorOr<CategoryDto>> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate Parent Category exists
        if (request.ParentCategoryId.HasValue)
        {
            var parent = await _repository.GetByIdAsync(request.ParentCategoryId.Value);
            if (parent is null)
                return Error.NotFound(code: "Category.ParentNotFound", description: "Parent category not found.");
        }

        // 2. Create Domain Model
        var category = new Domain.Entities.Category
        {
            Id = Guid.CreateVersion7(),
            Name = request.Name,
            Description = request.Description,
            IsActive = true,
            ParentCategoryId = request.ParentCategoryId
        };

        // 3. Save to DB
        var created = await _repository.CreateAsync(category);
        await _repository.SaveChangesAsync();

        // 4. Return DTO Response
        return new CategoryDto(
            created.Id,
            created.Name,
            created.Description,
            created.ParentCategoryId,
            Enumerable.Empty<CategoryDto>().ToList()
        );
    }
}