using ECommerce.Application.Categories.DTOs;
using ECommerce.Application.Cores.Abstractions;
using ErrorOr;

namespace ECommerce.Application.Features.Category.Command.CreateCategory;

public record CreateCategoryCommand(
    string Name,
    string? Description,
    bool IsActive = true
) : ICommand<ErrorOr<CategoryDto>>;