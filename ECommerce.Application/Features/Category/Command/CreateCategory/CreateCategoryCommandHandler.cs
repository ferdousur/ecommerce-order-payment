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
        var category = new Domain.Entities.Category
        {
            Id = Guid.CreateVersion7(),
            Name = request.Name,
            Description = request.Description,
            IsActive = request.IsActive
        };

        var created = await _repository.CreateAsync(category);
        await _repository.SaveChangesAsync();

        return new CategoryDto(created.Id, created.Name, created.Description, created.IsActive);
    }
}