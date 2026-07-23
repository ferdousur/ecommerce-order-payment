namespace ECommerce.Application.DTOs.Cart;

public record CartItemDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    decimal TotalPrice
);
