namespace ECommerce.Application.Checkout.DTOs;

using ECommerce.Domain.Enums;

public record CheckoutRequest(
    Guid CartId,
    PaymentProvider PaymentProvider,
    string ShippingAddress
);