using ECommerce.Application.Interfaces;
using ECommerce.Application.Payments.DTOs;
using ECommerce.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Stripe;

namespace ECommerce.Infrastructure.Payments;

public class StripePaymentProcessor : IPaymentProcessor
{
    private readonly IConfiguration _configuration;

    public PaymentProvider Provider => PaymentProvider.Stripe;


    public StripePaymentProcessor(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request)
    {
        try
        {

            var secretKey = _configuration["Stripe:SecretKey"];
            StripeConfiguration.ApiKey = secretKey;

            var options = new PaymentIntentCreateOptions
            {
                Amount = (long)(request.Amount * 100), 
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
        catch (Exception ex)
        {
            return new PaymentResult(
                IsSuccess: false,
                ErrorMessage: ex.Message
            );
        }
    }

    public Task<PaymentResult> CompletePaymentAsync(string paymentId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}