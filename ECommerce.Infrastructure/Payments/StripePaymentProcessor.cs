
using ECommerce.Application.Payments.DTOs;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Enums;
using Stripe;

namespace ECommerce.Infrastructure.Payments;

public class StripePaymentProcessor : IPaymentProcessor
{
    public PaymentProvider Provider => PaymentProvider.Stripe;

    public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request)
    {
        try
        {
            var options = new PaymentIntentCreateOptions
            {
                Amount = (long)(request.Amount * 100), // Convert Dollars to Cents
                Currency = request.Currency.ToLower(),
                PaymentMethodTypes = new List<string> { "card" },
                Metadata = new Dictionary<string, string>
                {
                    { "OrderId", request.OrderId.ToString() }
                }
            };

            var service = new PaymentIntentService();
            PaymentIntent intent = await service.CreateAsync(options);

            return new PaymentResult(
                IsSuccess: true,
                TransactionId: intent.Id,
                ClientSecret: intent.ClientSecret,
                RawResponse: intent.StripeResponse?.Content ?? string.Empty
            );
        }
        catch (StripeException ex)
        {
            return new PaymentResult(
                IsSuccess: false,
                ErrorMessage: ex.Message
            );
        }
    }
}