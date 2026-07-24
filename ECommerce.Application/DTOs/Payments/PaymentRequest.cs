namespace ECommerce.Application.Payments.DTOs;

public record PaymentRequest(
    Guid OrderId,
    decimal Amount,
    string Currency = "usd"
);

