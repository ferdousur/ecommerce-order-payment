namespace ECommerce.Application.Payments.DTOs;

public record PaymentResult(
    bool IsSuccess,
    string? TransactionId = null, 
    string? ClientSecret = null,  
    string? RedirectUrl = null,   
    string? ErrorMessage = null,
    string RawResponse = ""       
);