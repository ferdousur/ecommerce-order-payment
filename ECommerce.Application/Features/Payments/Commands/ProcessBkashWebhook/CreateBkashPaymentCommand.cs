using ECommerce.Application.Payments.DTOs;
using ErrorOr;
using MediatR;

namespace ECommerce.Application.Features.Payments.Commands.CreateBkashPayment;

public record CreateBkashPaymentCommand(
    Guid OrderId,
    decimal Amount
) : IRequest<ErrorOr<PaymentResult>>;