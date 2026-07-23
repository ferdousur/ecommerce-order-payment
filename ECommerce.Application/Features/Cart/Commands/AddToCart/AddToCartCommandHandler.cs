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

    public AddToCartCommandHandler(
        IRepository<Domain.Entities.Cart> cartRepository,
        IRepository<Domain.Entities.Product> productRepository)
    {
        _cartRepository = cartRepository;
        _productRepository = productRepository;
    }

    public async Task<ErrorOr<CartResponseDto>> Handle(AddToCartCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate Product & Stock
        var product = await _productRepository.GetByIdAsync(request.ProductId);
        if (product is null || !product.IsActive)
        {
            return Error.NotFound("Product.NotFound", "Product was not found or is inactive.");
        }

        if (product.Stock < request.Quantity)
        {
            return Error.Validation("Cart.StockExceeded", $"Insufficient stock. Only {product.Stock} available.");
        }

        // 2. Fetch Existing Cart for UserProfile
        var allCarts = await _cartRepository.GetAllAsync();
        var cart = allCarts.FirstOrDefault(c => c.UserProfileId == request.UserProfileId);

        if (cart is null)
        {
            // First time cart creation
            cart = new Domain.Entities.Cart
            {
                Id = Guid.CreateVersion7(),
                UserProfileId = request.UserProfileId,
                CreatedAt = DateTime.UtcNow,
                CartItems = new List<CartItem>()
            };
            await _cartRepository.CreateAsync(cart);
        }

        // 3. Add or Update Item in Cart
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

        // 4. Map & Return Response
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