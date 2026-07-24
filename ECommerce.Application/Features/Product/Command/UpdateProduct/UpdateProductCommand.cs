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
    List<Guid> CategoryIds,
    bool IsActive
) : ICommand<ErrorOr<ProductResponseDto>>;