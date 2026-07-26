
using ECommerce.Application.Payments.DTOs;
using ECommerce.Domain.Enums;


namespace ECommerce.Application.Interfaces;

public interface IPaymentProcessor
{
    PaymentProvider Provider { get; }
    Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request);
    Task<PaymentResult> CompletePaymentAsync(string paymentId, CancellationToken cancellationToken = default);
}