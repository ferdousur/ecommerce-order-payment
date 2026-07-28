using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Features.Payments.Commands.ExecuteBkashPayment;

public class ExecuteBkashPaymentCommandHandler : IRequestHandler<ExecuteBkashPaymentCommand, ErrorOr<Success>>
{
    private readonly IEnumerable<IPaymentProcessor> _paymentProcessors;
    private readonly IRepository<Order> _orderRepository;
    private readonly IRepository<Domain.Entities.Product> _productRepository;
    private readonly IRepository<Domain.Entities.Cart> _cartRepository;

    public ExecuteBkashPaymentCommandHandler(
        IEnumerable<IPaymentProcessor> paymentProcessors,
        IRepository<Order> orderRepository,
        IRepository<Domain.Entities.Product> productRepository,
        IRepository<Domain.Entities.Cart> _cartRepository)
    {
        _paymentProcessors = paymentProcessors;
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        this._cartRepository = _cartRepository;
    }

    public async Task<ErrorOr<Success>> Handle(ExecuteBkashPaymentCommand request, CancellationToken cancellationToken)
    {
        // 1. Resolve bKash payment processor
        var processor = _paymentProcessors.FirstOrDefault(p => p.Provider == PaymentProvider.Bkash);
        if (processor == null)
        {
            return Error.NotFound("Payment.ProcessorNotFound", "bKash processor is not configured.");
        }

        // 2. Fetch order along with items and payment details
        var order = await _orderRepository.GetQueryable()
            .Include(o => o.OrderItems)
            .Include(o => o.Payment)
            .FirstOrDefaultAsync(o => o.Payment != null && o.Payment.TransactionId == request.PaymentId, cancellationToken);

        if (order == null)
        {
            return Error.NotFound("Order.NotFound", "Order for this payment was not found.");
        }

        // 3. Return success if already paid to prevent duplicate processing
        if (order.Status == OrderStatus.Paid)
        {
            return Result.Success;
        }

        // 4. Execute bKash payment via processor
        var executeResult = await processor.CompletePaymentAsync(request.PaymentId, cancellationToken);
        if (!executeResult.IsSuccess)
        {
            order.Payment!.Status = PaymentStatus.Failed;
            order.Status = OrderStatus.Failed;
            await _orderRepository.SaveChangesAsync();

            return Error.Failure("Payment.ExecutionFailed", executeResult.ErrorMessage ?? "bKash payment execution failed.");
        }

        // 5. Update order and payment status to success
        order.Status = OrderStatus.Paid;
        if (order.Payment != null)
        {
            order.Payment.Status = PaymentStatus.Success;
            order.Payment.TransactionId = executeResult.TransactionId ?? request.PaymentId;
        }

        // 6. Reduce stock for each purchased product and track out-of-stock items
        var purchasedProductIds = new List<Guid>();
        foreach (var item in order.OrderItems)
        {
            var product = await _productRepository.GetQueryable()
                .FirstOrDefaultAsync(p => p.Id == item.ProductId, cancellationToken);

            if (product != null)
            {
                product.Stock = Math.Max(0, product.Stock - item.Quantity);
                await _productRepository.UpdateAsync(product);

                // Track if product is now out of stock
                if (product.Stock <= 0)
                {
                    purchasedProductIds.Add(product.Id);
                }
            }
        }

        // 7. Clear the buyer's active cart after successful payment
        var cart = await _cartRepository.GetQueryable()
            .FirstOrDefaultAsync(c => c.UserProfileId == order.UserProfileId, cancellationToken);

        if (cart != null)
        {
            await _cartRepository.DeleteAsync(cart.Id);
        }

        // 8. Automatically remove out-of-stock items from other users' carts
        if (purchasedProductIds.Any())
        {
            var otherCartsWithOutOfStockItems = await _cartRepository.GetQueryable()
                .Include(c => c.CartItems)
                .Where(c => c.CartItems.Any(ci => purchasedProductIds.Contains(ci.ProductId)))
                .ToListAsync(cancellationToken);

            foreach (var otherCart in otherCartsWithOutOfStockItems)
            {
                var itemsToRemove = otherCart.CartItems
                    .Where(ci => purchasedProductIds.Contains(ci.ProductId))
                    .ToList();

                foreach (var cartItem in itemsToRemove)
                {
                    otherCart.CartItems.Remove(cartItem);
                }

                await _cartRepository.UpdateAsync(otherCart);
            }
        }

        // 9. Persist all database changes atomically with Concurrency Exception Handling
        try
        {
            await _orderRepository.SaveChangesAsync();
            await _productRepository.SaveChangesAsync();
            await _cartRepository.SaveChangesAsync();

            return Result.Success;
        }
        catch (DbUpdateConcurrencyException)
        {
            // Mark order as failed due to concurrency conflict / race condition
            order.Status = OrderStatus.Failed;
            if (order.Payment != null)
            {
                order.Payment.Status = PaymentStatus.Failed;
            }

            await _orderRepository.SaveChangesAsync();

            return Error.Failure("Order.ConcurrencyConflict", "A concurrency conflict occurred while processing the order. The item might have just gone out of stock.");
        }
    }
}