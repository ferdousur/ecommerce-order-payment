using ECommerce.Application.Cores.Abstractions;
using ECommerce.Application.Products.DTOs;
using ErrorOr;

namespace ECommerce.Application.Features.Product.Command.UpdateProduct;

public record UpdateProductCommand(
    Guid Id,
    string Name,
    string? Description,
    decimal Price,
    int StockQuantity,
    string? SKU,
    Guid CategoryId,
    bool IsActive
) : ICommand<ErrorOr<ProductResponseDto>>;