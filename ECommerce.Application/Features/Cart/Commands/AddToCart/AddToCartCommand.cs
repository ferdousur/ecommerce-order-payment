using ECommerce.Application.Cores.Abstractions;
using ECommerce.Application.DTOs.Cart;
using ErrorOr;

namespace ECommerce.Application.Features.Cart.Commands.AddToCart;

public record AddToCartCommand(
    Guid UserProfileId,
    Guid ProductId,
    int Quantity
) : ICommand<ErrorOr<CartResponseDto>>;