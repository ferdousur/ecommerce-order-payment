namespace ECommerce.Application.Products.DTOs;

public record ProductResponseDto(
    Guid Id,
    string Name,
    string? Description,
    decimal Price,
    int StockQuantity,
    string? SKU,
    bool IsActive,
    Guid CategoryId,
    string? CategoryName,
    DateTime CreatedAtUtc
);