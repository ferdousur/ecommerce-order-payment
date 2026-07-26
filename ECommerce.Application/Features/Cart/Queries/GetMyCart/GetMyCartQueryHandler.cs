using ECommerce.Application.Cores.Abstractions;
using ECommerce.Application.DTOs.Cart;
using ECommerce.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using ErrorOr;


namespace ECommerce.Application.Features.Cart.Queries.GetMyCart;

public class GetMyCartQueryHandler : IQueryHandler<GetMyCartQuery, ErrorOr<CartResponseDto>>
{
    private readonly IRepository<Domain.Entities.Cart> _cartRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetMyCartQueryHandler(IRepository<Domain.Entities.Cart> cartRepository, ICurrentUserService currentUserService)
    {
        _cartRepository = cartRepository;
        _currentUserService = currentUserService;
    }

    public async Task<ErrorOr<CartResponseDto>> Handle(GetMyCartQuery request, CancellationToken cancellationToken)
    {
        // get id from token
        var userId = _currentUserService.UserId;

        //  by IQueryable cert, items and product
        var cart = await _cartRepository.GetQueryable()
            .Include(c => c.CartItems)
            .ThenInclude(ci => ci.Product)
            .FirstOrDefaultAsync(c => c.UserProfileId == userId);

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