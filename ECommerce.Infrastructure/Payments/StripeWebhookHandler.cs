using Microsoft.Extensions.Configuration;
using ECommerce.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using Stripe;

namespace ECommerce.Infrastructure.Payments;

public class StripeWebhookHandler : IPaymentWebhookHandler
{
    private readonly IRepository<Order> _orderRepository;
    private readonly IRepository<Domain.Entities.Product> _productRepository;
    private readonly IRepository<Domain.Entities.Cart> _cartRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<StripeWebhookHandler> _logger;

    public StripeWebhookHandler(
        IRepository<Order> orderRepository,
        IRepository<Domain.Entities.Product> productRepository,
        IRepository<Domain.Entities.Cart> cartRepository,
        IConfiguration configuration,
        ILogger<StripeWebhookHandler> logger)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _cartRepository = cartRepository;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> ProcessWebhookAsync(string jsonPayload, string signatureHeader, CancellationToken cancellationToken)
    {
        var webhookSecret = _configuration["Stripe:WebhookSecret"];

        try
        {
            var stripeEvent = EventUtility.ConstructEvent(jsonPayload, signatureHeader, webhookSecret);

            // 1. Payment Successful Handling
            if (stripeEvent.Type == EventTypes.PaymentIntentSucceeded)
            {
                if (stripeEvent.Data.Object is PaymentIntent paymentIntent)
                {
                    await HandleSuccessfulPaymentAsync(paymentIntent, cancellationToken);
                }
            }
            // 2. Payment Failed Handling
            else if (stripeEvent.Type == EventTypes.PaymentIntentPaymentFailed)
            {
                if (stripeEvent.Data.Object is PaymentIntent paymentIntent)
                {
                    await HandleFailedPaymentAsync(paymentIntent, cancellationToken);
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while verifying/processing Stripe webhook event.");
            return false;
        }
    }

    private async Task HandleSuccessfulPaymentAsync(PaymentIntent paymentIntent, CancellationToken cancellationToken)
    {
        var orderIdKey = paymentIntent.Metadata?.Keys
            .FirstOrDefault(k => k.Equals("OrderId", StringComparison.OrdinalIgnoreCase));

        if (orderIdKey != null && paymentIntent.Metadata!.TryGetValue(orderIdKey, out string? orderIdStr))
        {
            if (Guid.TryParse(orderIdStr, out Guid orderId))
            {
                await ProcessOrderAndStockAsync(orderId, paymentIntent.Id, cancellationToken);
            }
        }
    }

    private async Task HandleFailedPaymentAsync(PaymentIntent paymentIntent, CancellationToken cancellationToken)
    {
        var orderIdKey = paymentIntent.Metadata?.Keys
            .FirstOrDefault(k => k.Equals("OrderId", StringComparison.OrdinalIgnoreCase));

        if (orderIdKey != null && paymentIntent.Metadata!.TryGetValue(orderIdKey, out string? orderIdStr))
        {
            if (Guid.TryParse(orderIdStr, out Guid orderId))
            {
                await UpdateFailedOrderStatusAsync(orderId, paymentIntent.Id, cancellationToken);
            }
        }
    }

    private async Task ProcessOrderAndStockAsync(Guid orderId, string transactionId, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetQueryable()
            .Include(o => o.OrderItems)
            .Include(o => o.Payment)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order != null)
        {
            if (order.Status == OrderStatus.Paid || (order.Payment != null && order.Payment.Status == PaymentStatus.Success))
            {
                _logger.LogInformation("Order {OrderId} is already processed as Paid.", orderId);
                return;
            }

            order.Status = OrderStatus.Paid;
            if (order.Payment != null)
            {
                order.Payment.Status = PaymentStatus.Success;
                order.Payment.TransactionId = transactionId;
            }

            var purchasedProductIds = new List<Guid>();

            if (order.OrderItems != null && order.OrderItems.Any())
            {
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

                        if (product.Stock < 0)
                        {
                            _logger.LogWarning("Product {ProductId} stock dropped below zero!", product.Id);
                        }
                    }
                }
            }

            // 1. Clear the buyer's active cart after successful payment
            var buyerCart = await _cartRepository.GetQueryable()
                .FirstOrDefaultAsync(c => c.UserProfileId == order.UserProfileId, cancellationToken);

            if (buyerCart != null)
            {
                await _cartRepository.DeleteAsync(buyerCart.Id);
            }

            // 2. Automatically remove out-of-stock items from other users' carts
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

            // 3. Persist all database changes atomically with Concurrency Exception Handling
            try
            {
                await _orderRepository.SaveChangesAsync();
                await _productRepository.SaveChangesAsync();
                await _cartRepository.SaveChangesAsync();

                _logger.LogInformation("Order {OrderId} payment processed, stock deducted, and carts synchronized successfully.", orderId);
            }
            catch (DbUpdateConcurrencyException dbEx)
            {
                _logger.LogWarning(dbEx, "Concurrency conflict detected while updating stock/order {OrderId}. Another transaction modified the product.", orderId);

                // Mark order as failed due to stock conflict / race condition
                order.Status = OrderStatus.Failed;
                if (order.Payment != null)
                {
                    order.Payment.Status = PaymentStatus.Failed;
                }

                await _orderRepository.SaveChangesAsync();
            }
        }
    }

    private async Task UpdateFailedOrderStatusAsync(Guid orderId, string transactionId, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetQueryable()
            .Include(o => o.Payment)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order != null)
        {
            order.Status = OrderStatus.Failed;
            if (order.Payment != null)
            {
                order.Payment.Status = PaymentStatus.Failed;
                order.Payment.TransactionId = transactionId;
            }

            await _orderRepository.SaveChangesAsync();
        }
    }
}