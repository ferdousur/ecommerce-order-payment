using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;

namespace ECommerce.Infrastructure.Payments;

public class StripeWebhookHandler : IPaymentWebhookHandler
{
    private readonly IRepository<Order> _orderRepository;
    private readonly IRepository<Domain.Entities.Product> _productRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<StripeWebhookHandler> _logger;

    public StripeWebhookHandler(
        IRepository<Order> orderRepository,
        IRepository<Domain.Entities.Product> productRepository,
        IConfiguration configuration,
        ILogger<StripeWebhookHandler> logger)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> ProcessWebhookAsync(string jsonPayload, string signatureHeader, CancellationToken cancellationToken)
    {
        var webhookSecret = _configuration["Stripe:WebhookSecret"];

        try
        {
            var stripeEvent = EventUtility.ConstructEvent(
                jsonPayload,
                signatureHeader,
                webhookSecret,
                throwOnApiVersionMismatch: false);

            // 1. Payment Successful Handling
            if (stripeEvent.Type == EventTypes.PaymentIntentSucceeded)
            {
                var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                if (paymentIntent != null)
                {
                    await HandleSuccessfulPaymentAsync(paymentIntent, cancellationToken);
                }
            }
            // 2. Payment Failed Handling
            else if (stripeEvent.Type == EventTypes.PaymentIntentPaymentFailed)
            {
                var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                if (paymentIntent != null)
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

            if (order.OrderItems != null && order.OrderItems.Any())
            {
                foreach (var item in order.OrderItems)
                {
                    var product = await _productRepository.GetQueryable()
                        .FirstOrDefaultAsync(p => p.Id == item.ProductId, cancellationToken);

                    if (product != null)
                    {
                        product.Stock -= item.Quantity;

                        if (product.Stock < 0)
                        {
                            _logger.LogWarning("Product {ProductId} stock dropped below zero!", product.Id);
                        }
                    }
                }
            }

            await _orderRepository.SaveChangesAsync();
            await _productRepository.SaveChangesAsync();

            _logger.LogInformation("Order {OrderId} payment processed and stock deducted successfully.", orderId);
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