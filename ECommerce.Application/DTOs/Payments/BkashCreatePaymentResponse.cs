namespace ECommerce.Application.Payments.DTOs;

public record BkashCreatePaymentResponse(
    string PaymentID,
    string BkashURL,
    string CallbackURL,
    string Amount,
    string Currency,
    string Intent,
    string StatusCode,
    string StatusMessage
);