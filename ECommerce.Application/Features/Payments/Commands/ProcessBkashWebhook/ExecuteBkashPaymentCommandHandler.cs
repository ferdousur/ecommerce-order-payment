using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Features.Payments.Commands.ExecuteBkashPayment;

public class ExecuteBkashPaymentCommandHandler : IRequestHandler<ExecuteBkashPaymentCommand, ErrorOr<Success>>
{
    private readonly IEnumerable<IPaymentProcessor> _paymentProcessors;
    private readonly IRepository<Order> _orderRepository;
    private readonly IRepository<Domain.Entities.Product> _productRepository;
    private readonly IRepository<Domain.Entities.Cart> _cartRepository;

    public ExecuteBkashPaymentCommandHandler(
        IEnumerable<IPaymentProcessor> paymentProcessors,
        IRepository<Order> orderRepository,
        IRepository<Domain.Entities.Product> productRepository,
        IRepository<Domain.Entities.Cart> cartRepository)
    {
        _paymentProcessors = paymentProcessors;
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _cartRepository = cartRepository;
    }

    public async Task<ErrorOr<Success>> Handle(ExecuteBkashPaymentCommand request, CancellationToken cancellationToken)
    {
        var processor = _paymentProcessors.FirstOrDefault(p => p.Provider == PaymentProvider.Bkash);
        if (processor == null)
        {
            return Error.NotFound("Payment.ProcessorNotFound", "bKash processor is not configured.");
        }


        var order = await _orderRepository.GetQueryable()
            .Include(o => o.OrderItems)
            .Include(o => o.Payment)
            .FirstOrDefaultAsync(o => o.Payment != null && o.Payment.TransactionId == request.PaymentId, cancellationToken);

        if (order == null)
        {
            return Error.NotFound("Order.NotFound", "Order for this payment was not found.");
        }

        if (order.Status == OrderStatus.Paid)
        {
            return Result.Success;
        }


        var executeResult = await processor.CompletePaymentAsync(request.PaymentId, cancellationToken);
        if (!executeResult.IsSuccess)
        {
            order.Payment!.Status = PaymentStatus.Failed;
            order.Status = OrderStatus.Failed;
            await _orderRepository.SaveChangesAsync();

            return Error.Failure("Payment.ExecutionFailed", executeResult.ErrorMessage ?? "bKash payment execution failed.");
        }



        order.Status = OrderStatus.Paid;
        if (order.Payment != null)
        {
            order.Payment.Status = PaymentStatus.Success;
            order.Payment.TransactionId = executeResult.TransactionId ?? request.PaymentId;
        }


        foreach (var item in order.OrderItems)
        {
            var product = await _productRepository.GetQueryable()
                .FirstOrDefaultAsync(p => p.Id == item.ProductId, cancellationToken);

            if (product != null)
            {
                product.Stock = Math.Max(0, product.Stock - item.Quantity);
            }
        }


        var cart = await _cartRepository.GetQueryable()
            .FirstOrDefaultAsync(c => c.UserProfileId == order.UserProfileId, cancellationToken);

        if (cart != null)
        {
            await _cartRepository.DeleteAsync(cart.Id);
            await _cartRepository.SaveChangesAsync();
        }

        await _orderRepository.SaveChangesAsync();
        await _productRepository.SaveChangesAsync();

        return Result.Success;
    }
}