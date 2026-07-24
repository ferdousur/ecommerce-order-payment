namespace ECommerce.Application.Interfaces;

using ECommerce.Application.Payments.DTOs;
using ECommerce.Domain.Enums;

public interface IPaymentProcessor
{
    PaymentProvider Provider { get; }
    Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request);
}