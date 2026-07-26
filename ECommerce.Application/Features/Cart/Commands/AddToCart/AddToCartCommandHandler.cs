using ECommerce.Application.Cores.Abstractions;
using ECommerce.Application.DTOs.Cart;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ErrorOr;

namespace ECommerce.Application.Features.Cart.Commands.AddToCart;

public class AddToCartCommandHandler : ICommandHandler<AddToCartCommand, ErrorOr<CartResponseDto>>
{
    private readonly IRepository<Domain.Entities.Cart> _cartRepository;
    private readonly IRepository<Domain.Entities.Product> _productRepository;
    private readonly ICurrentUserService _currentUserService;

    public AddToCartCommandHandler(
        IRepository<Domain.Entities.Cart> cartRepository,
        IRepository<Domain.Entities.Product> productRepository,
        ICurrentUserService currentUserService)
    {
        _cartRepository = cartRepository;
        _productRepository = productRepository;
        _currentUserService = currentUserService;
    }

    public async Task<ErrorOr<CartResponseDto>> Handle(AddToCartCommand request, CancellationToken cancellationToken)
    {
        // 1. Id Get from Token
        var userProfileId = _currentUserService.UserId;
        if (userProfileId is null || userProfileId == Guid.Empty)
        {
            return Error.Unauthorized("User.Unauthorized", "User is not authenticated or token is invalid.");
        }

        // 2. Validate Product & Stock
        var product = await _productRepository.GetByIdAsync(request.ProductId);
        if (product is null || !product.IsActive)
        {
            return Error.NotFound("Product.NotFound", "Product was not found or is inactive.");
        }

        if (product.Stock < request.Quantity)
        {
            return Error.Validation("Cart.StockExceeded", $"Insufficient stock. Only {product.Stock} available.");
        }

        // 3. Fetch Existing Cart for UserProfile 
        var allCarts = await _cartRepository.GetAllAsync();
        var cart = allCarts.FirstOrDefault(c => c.UserProfileId == userProfileId.Value);

        if (cart is null)
        {
            cart = new Domain.Entities.Cart
            {
                Id = Guid.CreateVersion7(),
                UserProfileId = userProfileId.Value,
                CreatedAt = DateTime.UtcNow,
                CartItems = new List<CartItem>()
            };
            await _cartRepository.CreateAsync(cart);
        }

        // 4. Add or Update Item in Cart 
        var existingItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == request.ProductId);
        if (existingItem is not null)
        {
            existingItem.Quantity += request.Quantity;
            existingItem.UnitPriceAtAdd = product.Price;
        }
        else
        {
            cart.CartItems.Add(new CartItem
            {
                Id = Guid.CreateVersion7(),
                CartId = cart.Id,
                ProductId = request.ProductId,
                Quantity = request.Quantity,
                UnitPriceAtAdd = product.Price
            });
        }

        cart.UpdatedAt = DateTime.UtcNow;
        await _cartRepository.SaveChangesAsync();

        // 5. Map & Return Response
        var itemDtos = cart.CartItems.Select(item => new CartItemDto(
            item.Id,
            item.ProductId,
            product.Id == item.ProductId ? product.Name : "Product",
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