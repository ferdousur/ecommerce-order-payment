using ECommerce.Application.Interfaces;
using ErrorOr;
using MediatR;

namespace ECommerce.Application.Features.Payments.Commands.ProcessStripeWebhook;

public class ProcessStripeWebhookCommandHandler : IRequestHandler<ProcessStripeWebhookCommand, ErrorOr<Success>>
{
    private readonly IPaymentWebhookHandler _webhookHandler;

    public ProcessStripeWebhookCommandHandler(IPaymentWebhookHandler webhookHandler)
    {
        _webhookHandler = webhookHandler;
    }

    public async Task<ErrorOr<Success>> Handle(ProcessStripeWebhookCommand request, CancellationToken cancellationToken)
    {
        var isSuccess = await _webhookHandler.ProcessWebhookAsync(request.JsonPayload, request.SignatureHeader, cancellationToken);

        if (!isSuccess)
        {
            return Error.Failure("Webhook.ProcessingFailed", "Failed to process payment webhook.");
        }

        return Result.Success;
    }
}