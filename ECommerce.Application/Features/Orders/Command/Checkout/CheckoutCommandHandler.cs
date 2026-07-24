using ECommerce.Application.Checkout.DTOs;
using ECommerce.Application.Cores.Abstractions;
using ECommerce.Application.Interfaces;
using ECommerce.Application.Payments.DTOs;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ErrorOr;

namespace ECommerce.Application.Features.Orders.Commands.Checkout;

public class CheckoutCommandHandler : ICommandHandler<CheckoutCommand, ErrorOr<CheckoutResponse>>
{
    private readonly IRepository<Domain.Entities.Cart> _cartRepository;
    private readonly IRepository<Order> _orderRepository;
    private readonly IEnumerable<IPaymentProcessor> _paymentProcessors;

    public CheckoutCommandHandler(
        IRepository<Domain.Entities.Cart> cartRepository,
        IRepository<Order> orderRepository,
        IEnumerable<IPaymentProcessor> paymentProcessors)
    {
        _cartRepository = cartRepository;
        _orderRepository = orderRepository;
        _paymentProcessors = paymentProcessors;
    }

    public async Task<ErrorOr<CheckoutResponse>> Handle(CheckoutCommand request, CancellationToken cancellationToken)
    {
        // 1. Fetch Cart
        var allCarts = await _cartRepository.GetAllAsync();
        var cart = allCarts.FirstOrDefault(c => c.Id == request.CartId && c.UserProfileId == request.UserProfileId);

        if (cart is null || cart.CartItems is null || !cart.CartItems.Any())
        {
            return Error.NotFound("Cart.NotFound", "Cart is empty or was not found.");
        }

        // 2. Calculate Total Amount
        decimal totalAmount = cart.CartItems.Sum(item => item.Quantity * item.UnitPriceAtAdd);
        if (totalAmount <= 0)
        {
            return Error.Validation("Cart.InvalidAmount", "Total amount must be greater than zero.");
        }

        // 3. Create Order Entity
        var order = new Order
        {
            Id = Guid.CreateVersion7(),
            UserProfileId = request.UserProfileId,
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

        // 4. Create Payment Entity
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

        // 5. Select Strategy
        var processor = _paymentProcessors.FirstOrDefault(p => p.Provider == request.PaymentProvider);
        if (processor is null)
        {
            return Error.Failure("Payment.UnsupportedProvider", $"Payment provider '{request.PaymentProvider}' is not supported.");
        }

        // 6. Execute Payment Request
        var paymentRequest = new PaymentRequest(order.Id, totalAmount, "usd");
        var paymentResult = await processor.ProcessPaymentAsync(paymentRequest);

        if (!paymentResult.IsSuccess)
        {
            payment.Status = PaymentStatus.Failed;
            order.Status = OrderStatus.Failed;
            await _orderRepository.SaveChangesAsync();

            return Error.Failure("Payment.Failed", paymentResult.ErrorMessage ?? "Payment processing failed.");
        }

        // 7. Update Payment Entity with Transaction details
        payment.TransactionId = paymentResult.TransactionId;
        payment.RawResponse = paymentResult.RawResponse;
        await _orderRepository.SaveChangesAsync();

        // 8. Return Response
        return new CheckoutResponse(
            OrderId: order.Id,
            OrderStatus: order.Status,
            PaymentStatus: payment.Status,
            ClientSecret: paymentResult.ClientSecret,
            RedirectUrl: paymentResult.RedirectUrl
        );
    }
}