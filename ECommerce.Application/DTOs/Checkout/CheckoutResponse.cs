namespace ECommerce.Application.Checkout.DTOs;

using ECommerce.Domain.Enums;

public record CheckoutResponse(
    Guid OrderId,
    OrderStatus OrderStatus,
    PaymentStatus PaymentStatus,
    string? ClientSecret,   
    string? RedirectUrl     
);