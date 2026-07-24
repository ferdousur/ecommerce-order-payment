using ErrorOr;
using MediatR;

namespace ECommerce.Application.Features.Payments.Commands.ProcessStripeWebhook;

public record ProcessStripeWebhookCommand(
    string JsonPayload,
    string SignatureHeader
) : IRequest<ErrorOr<Success>>;