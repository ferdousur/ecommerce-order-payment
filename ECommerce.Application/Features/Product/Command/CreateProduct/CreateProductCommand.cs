using ECommerce.Application.Categories.DTOs;
using ECommerce.Application.Cores.Abstractions;
using ECommerce.Application.DTOs.Product;
using ErrorOr;

namespace ECommerce.Application.Features.Category.Command.CreateProduct;

public record CreateProductCommand(
    string Name,
    string? Description,
    decimal Price,
    int StockQuantity,
    string? SKU,
    Guid CategoryId,
    bool IsActive = true
) : ICommand<ErrorOr<CreateProductDto>>;