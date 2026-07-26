using ECommerce.Application.Cores.Abstractions;
using ECommerce.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ErrorOr;

using PaymentRequest = ECommerce.Application.Payments.DTOs.PaymentRequest;
using CheckoutResponse = ECommerce.Application.Checkout.DTOs.CheckoutResponse;

namespace ECommerce.Application.Features.Orders.Commands.Checkout;

public class CheckoutCommandHandler : ICommandHandler<CheckoutCommand, ErrorOr<CheckoutResponse>>
{
    private readonly IRepository<Domain.Entities.Cart> _cartRepository;
    private readonly IRepository<Order> _orderRepository;
    private readonly IEnumerable<IPaymentProcessor> _paymentProcessors;
    private readonly ICurrentUserService _currentUserService;

    public CheckoutCommandHandler(
        IRepository<Domain.Entities.Cart> cartRepository,
        IRepository<Order> orderRepository,
        IEnumerable<IPaymentProcessor> paymentProcessors,
        ICurrentUserService currentUserService)
    {
        _cartRepository = cartRepository;
        _orderRepository = orderRepository;
        _paymentProcessors = paymentProcessors;
        _currentUserService = currentUserService;
    }
    public async Task<ErrorOr<CheckoutResponse>> Handle(CheckoutCommand request, CancellationToken cancellationToken)
    {
        // 1. Get User ID from Token
        var userProfileId = _currentUserService.UserId;
        if (userProfileId is null || userProfileId == Guid.Empty)
        {
            return Error.Unauthorized("User.Unauthorized", "User is not authenticated or token is invalid.");
        }

        // 2. Get Cart directly using UserProfileId 
        var cart = await _cartRepository.GetQueryable()
            .Include(c => c.CartItems)
            .FirstOrDefaultAsync(c => c.UserProfileId == userProfileId.Value, cancellationToken);

        if (cart is null || cart.CartItems is null || !cart.CartItems.Any())
        {
            return Error.NotFound("Cart.NotFound", "Cart is empty or was not found.");
        }

        // 3. Calculate Total Amount
        decimal totalAmount = cart.CartItems.Sum(item => item.Quantity * item.UnitPriceAtAdd);
        if (totalAmount <= 0)
        {
            return Error.Validation("Cart.InvalidAmount", "Total amount must be greater than zero.");
        }

        // 4. Create Order Entity
        var order = new Order
        {
            Id = Guid.CreateVersion7(),
            UserProfileId = userProfileId.Value,
            Status = OrderStatus.Pending,
            TotalAmount = totalAmount,
            ShippingAddress = request.ShippingAddress,
            CreatedAt = DateTime.UtcNow,
            OrderItems = cart.CartItems.Select(ci => new OrderItem
            {
                Id = Guid.CreateVersion7(),
                ProductId = ci.ProductId,
                Quantity = ci.Quantity,
                Price = ci.UnitPriceAtAdd,
                Subtotal = ci.Quantity * ci.UnitPriceAtAdd
            }).ToList()
        };

        // 5. Create Payment Entity
        var payment = new Payment
        {
            Id = Guid.CreateVersion7(),
            OrderId = order.Id,
            Provider = request.PaymentProvider,
            Status = PaymentStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        order.Payment = payment;

        // Save initial Order & Payment
        await _orderRepository.CreateAsync(order);
        await _orderRepository.SaveChangesAsync();

        // 6. Select Strategy
        var processor = _paymentProcessors.FirstOrDefault(p => p.Provider == request.PaymentProvider);
        if (processor is null)
        {
            return Error.Failure("Payment.UnsupportedProvider", $"Payment provider '{request.PaymentProvider}' is not supported.");
        }

        // 7. Execute Payment Request
        var paymentRequest = new PaymentRequest(order.Id, totalAmount, "usd");
        var paymentResult = await processor.ProcessPaymentAsync(paymentRequest);

        if (!paymentResult.IsSuccess)
        {
            payment.Status = PaymentStatus.Failed;
            order.Status = OrderStatus.Failed;
            await _orderRepository.SaveChangesAsync();

            return Error.Failure("Payment.Failed", paymentResult.ErrorMessage ?? "Payment processing failed.");
        }

        // 8. Update Payment Entity with Transaction details
        payment.TransactionId = paymentResult.TransactionId;
        payment.RawResponse = paymentResult.RawResponse;

        // 9. Clear / Delete Cart 
        await _cartRepository.DeleteAsync(cart.Id);

        await _orderRepository.SaveChangesAsync();
        await _cartRepository.SaveChangesAsync();

        // 10. Return Response
        return new CheckoutResponse(
            OrderId: order.Id,
            OrderStatus: order.Status,
            PaymentStatus: payment.Status,
            ClientSecret: paymentResult.ClientSecret,
            RedirectUrl: paymentResult.RedirectUrl
        );
    }
}