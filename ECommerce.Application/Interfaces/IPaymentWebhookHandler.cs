namespace ECommerce.Application.Interfaces;

public interface IPaymentWebhookHandler
{
    Task<bool> ProcessWebhookAsync(string jsonPayload, string signatureHeader, CancellationToken cancellationToken);
}