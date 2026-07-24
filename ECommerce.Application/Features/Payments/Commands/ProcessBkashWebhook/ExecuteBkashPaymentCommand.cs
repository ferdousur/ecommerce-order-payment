using ErrorOr;
using MediatR;

namespace ECommerce.Application.Features.Payments.Commands.ExecuteBkashPayment;

public record ExecuteBkashPaymentCommand(
    string PaymentId
) : IRequest<ErrorOr<Success>>;