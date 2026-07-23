namespace ECommerce.Application.DTOs.Cart;

public record CartResponseDto(
    Guid Id,
    Guid UserProfileId,
    List<CartItemDto> CartItems,
    decimal GrandTotal
);