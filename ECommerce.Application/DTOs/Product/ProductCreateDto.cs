namespace ECommerce.Application.DTOs.Product;

public record CreateProductDto(
    string Name,
    string? Description,
    decimal Price,
    int StockQuantity,
    string? SKU,
    Guid CategoryId,
    bool IsActive = true
);