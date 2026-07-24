using ECommerce.Application.Cores.Abstractions;
using ECommerce.Application.Products.DTOs;
using ErrorOr;

namespace ECommerce.Application.Features.Product.Command.CreateProduct;

public record CreateProductCommand(
    string Name,
    string? Description,
    decimal Price,
    int StockQuantity,
    string? SKU,
    List<Guid> CategoryIds,
    bool IsActive = true
) : ICommand<ErrorOr<ProductResponseDto>>;