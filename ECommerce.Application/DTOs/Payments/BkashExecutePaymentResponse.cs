namespace ECommerce.Application.Payments.DTOs;

public record BkashExecutePaymentResponse(
    string PaymentID,
    string TrxID,
    string TransactionStatus,
    string Amount,
    string StatusCode,
    string StatusMessage
);