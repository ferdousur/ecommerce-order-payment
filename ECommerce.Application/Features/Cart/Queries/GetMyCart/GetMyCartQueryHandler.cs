using ECommerce.Application.Cores.Abstractions;
using ECommerce.Application.DTOs.Cart;
using ECommerce.Application.Interfaces;
using ErrorOr;

namespace ECommerce.Application.Features.Cart.Queries.GetMyCart;

public class GetMyCartQueryHandler : IQueryHandler<GetMyCartQuery, ErrorOr<CartResponseDto>>
{
    private readonly IRepository<Domain.Entities.Cart> _cartRepository;

    public GetMyCartQueryHandler(IRepository<Domain.Entities.Cart> cartRepository)
    {
        _cartRepository = cartRepository;
    }

    public async Task<ErrorOr<CartResponseDto>> Handle(GetMyCartQuery request, CancellationToken cancellationToken)
    {
        var allCarts = await _cartRepository.GetAllAsync();
        var cart = allCarts.FirstOrDefault(c => c.UserProfileId == request.UserProfileId);

        if (cart is null)
        {
            return Error.NotFound("Cart.NotFound", "Cart is empty for this user.");
        }

        var itemDtos = cart.CartItems.Select(item => new CartItemDto(
            item.Id,
            item.ProductId,
            item.Product?.Name ?? "Product",
            item.UnitPriceAtAdd,
            item.Quantity,
            item.UnitPriceAtAdd * item.Quantity
        )).ToList();

        return new CartResponseDto(
            cart.Id,
            cart.UserProfileId,
            itemDtos,
            itemDtos.Sum(x => x.TotalPrice)
        );
    }
}