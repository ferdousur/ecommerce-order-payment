namespace ECommerce.Application.Products.DTOs;

public record CreateProductDto(
    string Name,
    string? Description,
    decimal Price,
    int StockQuantity,
    string? SKU,
    List<Guid> CategoryIds,
    bool IsActive = true
);