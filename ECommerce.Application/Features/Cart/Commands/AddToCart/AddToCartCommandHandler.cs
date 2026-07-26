using ECommerce.Application.Cores.Abstractions;
using ECommerce.Application.DTOs.Cart;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using ErrorOr;

namespace ECommerce.Application.Features.Cart.Commands.AddToCart;

public class AddToCartCommandHandler : ICommandHandler<AddToCartCommand, ErrorOr<CartResponseDto>>
{
    private readonly IRepository<Domain.Entities.Cart> _cartRepository;
    private readonly IRepository<CartItem> _cartItemRepository;
    private readonly IRepository<Domain.Entities.Product> _productRepository;
    private readonly ICurrentUserService _currentUserService;

    public AddToCartCommandHandler(
        IRepository<Domain.Entities.Cart> cartRepository,
        IRepository<CartItem> cartItemRepository,
        IRepository<Domain.Entities.Product> productRepository,
        ICurrentUserService currentUserService)
    {
        _cartRepository = cartRepository;
        _cartItemRepository = cartItemRepository;
        _productRepository = productRepository;
        _currentUserService = currentUserService;
    }

    public async Task<ErrorOr<CartResponseDto>> Handle(AddToCartCommand request, CancellationToken cancellationToken)
    {
        // 1. Get User ID from Token & Validate
        var userProfileId = _currentUserService.UserId;
        if (userProfileId is null || userProfileId == Guid.Empty)
        {
            return Error.Unauthorized("User.Unauthorized", "User is not authenticated or token is invalid.");
        }

        // 2. Quantity Validation
        if (request.Quantity <= 0)
        {
            return Error.Validation("Cart.InvalidQuantity", "Quantity must be greater than zero.");
        }

        // 3. Validate Product & Stock
        var product = await _productRepository.GetByIdAsync(request.ProductId);
        if (product is null || !product.IsActive)
        {
            return Error.NotFound("Product.NotFound", "Product was not found or is inactive.");
        }

        if (product.Stock < request.Quantity)
        {
            return Error.Validation("Cart.StockExceeded", $"Insufficient stock. Only {product.Stock} available.");
        }

        // 4. Fetch Existing Cart for User
        var cart = await _cartRepository.GetQueryable()
            .Include(c => c.CartItems)
            .ThenInclude(ci => ci.Product)
            .FirstOrDefaultAsync(c => c.UserProfileId == userProfileId.Value, cancellationToken);

        if (cart is null)
        {
            cart = new Domain.Entities.Cart
            {
                Id = Guid.CreateVersion7(),
                UserProfileId = userProfileId.Value,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _cartRepository.CreateAsync(cart);
            await _cartRepository.SaveChangesAsync();
        }

        // 5. Check if item already exists in the cart
        var existingItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == request.ProductId);

        if (existingItem is not null)
        {
            // Update quantity of existing item
            existingItem.Quantity += request.Quantity;
            existingItem.UnitPriceAtAdd = product.Price;
            cart.UpdatedAt = DateTime.UtcNow;

            await _cartRepository.UpdateAsync(cart);
            await _cartRepository.SaveChangesAsync();
        }
        else
        {
            // Add brand new item safely using CartItem repository to avoid parent tracking conflict
            var newItem = new CartItem
            {
                Id = Guid.CreateVersion7(),
                CartId = cart.Id,
                ProductId = request.ProductId,
                Quantity = request.Quantity,
                UnitPriceAtAdd = product.Price
            };

            await _cartItemRepository.CreateAsync(newItem);

            cart.UpdatedAt = DateTime.UtcNow;
            await _cartRepository.UpdateAsync(cart);
            await _cartRepository.SaveChangesAsync();

            // Reload cart items to include the newly added item for response mapping
            cart = await _cartRepository.GetQueryable()
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserProfileId == userProfileId.Value, cancellationToken);
        }

        // 6. Map & Return Response
        var itemDtos = cart!.CartItems.Select(item => new CartItemDto(
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