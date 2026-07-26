using ECommerce.Application.Checkout.DTOs;
using ECommerce.Application.Cores.Abstractions;
using ECommerce.Domain.Enums;
using ErrorOr;

namespace ECommerce.Application.Features.Orders.Commands.Checkout;

public record CheckoutCommand(
    PaymentProvider PaymentProvider,
    string ShippingAddress
) : ICommand<ErrorOr<CheckoutResponse>>;