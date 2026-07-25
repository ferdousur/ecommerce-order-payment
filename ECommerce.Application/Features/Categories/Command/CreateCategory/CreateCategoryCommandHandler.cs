using ECommerce.Application.Categories.DTOs;
using ECommerce.Application.Cores.Abstractions;
using ECommerce.Application.Interfaces;
using ErrorOr;
using Microsoft.Extensions.Caching.Distributed;

namespace ECommerce.Application.Features.Category.Command.CreateCategory;

public class CreateCategoryCommandHandler : ICommandHandler<CreateCategoryCommand, ErrorOr<CategoryDto>>
{
    private readonly IRepository<Domain.Entities.Category> _repository;
    private readonly IDistributedCache _cache;

    public CreateCategoryCommandHandler(IRepository<Domain.Entities.Category> repository, IDistributedCache cache)
    {
        _repository = repository;
        _cache = cache;
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
        await _cache.RemoveAsync("categories_tree_dfs", cancellationToken);

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