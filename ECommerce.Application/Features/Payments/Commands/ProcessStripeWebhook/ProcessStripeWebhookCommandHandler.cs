using ECommerce.Application.Interfaces;
using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Features.Payments.Commands.ProcessStripeWebhook;

public class ProcessStripeWebhookCommandHandler : IRequestHandler<ProcessStripeWebhookCommand, ErrorOr<Success>>
{
    private readonly IPaymentWebhookHandler _webhookHandler;
    private readonly IRepository<Domain.Entities.Cart> _cartRepository;
    private readonly ICurrentUserService _currentUserService;

    public ProcessStripeWebhookCommandHandler(
        IPaymentWebhookHandler webhookHandler,
        IRepository<Domain.Entities.Cart> cartRepository,
        ICurrentUserService currentUserService)
    {
        _webhookHandler = webhookHandler;
        _cartRepository = cartRepository;
        _currentUserService = currentUserService;
    }

    public async Task<ErrorOr<Success>> Handle(ProcessStripeWebhookCommand request, CancellationToken cancellationToken)
    {

        bool isSuccess = await _webhookHandler.ProcessWebhookAsync(request.JsonPayload, request.SignatureHeader, cancellationToken);

        if (!isSuccess)
        {
            return Error.Failure("Webhook.ProcessingFailed", "Failed to process payment webhook.");
        }


        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            return Error.Failure("User.Unauthorized", "User is not authenticated.");
        }


        var cart = await _cartRepository.GetQueryable()
            .FirstOrDefaultAsync(c => c.UserProfileId == userId, cancellationToken);

        if (cart != null)
        {
            await _cartRepository.DeleteAsync(cart.Id);
            await _cartRepository.SaveChangesAsync();
        }

        return Result.Success;
    }
}