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

    public ExecuteBkashPaymentCommandHandler(
        IEnumerable<IPaymentProcessor> paymentProcessors,
        IRepository<Order> orderRepository,
        IRepository<Domain.Entities.Product> productRepository)
    {
        _paymentProcessors = paymentProcessors;
        _orderRepository = orderRepository;
        _productRepository = productRepository;
    }

    public async Task<ErrorOr<Success>> Handle(ExecuteBkashPaymentCommand request, CancellationToken cancellationToken)
    {

        var processor = _paymentProcessors.FirstOrDefault(p => p.Provider == PaymentProvider.Bkash);
        if (processor == null)
        {
            return Error.NotFound(
                code: "Payment.ProcessorNotFound",
                description: "bKash payment processor is not configured."
            );
        }


        var order = await _orderRepository.GetQueryable()
            .Include(o => o.OrderItems)
            .Include(o => o.Payment)
            .FirstOrDefaultAsync(o => o.Payment != null && o.Payment.TransactionId == request.PaymentId, cancellationToken);

        if (order == null)
        {
            return Error.NotFound(
                code: "Order.NotFound",
                description: $"Order associated with Payment ID {request.PaymentId} was not found."
            );
        }


        if (order.Status == OrderStatus.Paid || (order.Payment != null && order.Payment.Status == PaymentStatus.Success))
        {
            return Result.Success;
        }


        var executeResult = await processor.CompletePaymentAsync(request.PaymentId, cancellationToken);

        if (!executeResult.IsSuccess)
        {
            return Error.Failure(
                code: "Payment.ExecutionFailed",
                description: executeResult.ErrorMessage ?? "bKash payment execution failed."
            );
        }


        order.Status = OrderStatus.Paid;
        if (order.Payment != null)
        {
            order.Payment.Status = PaymentStatus.Success;
            // bKash Execute পর প্রাপ্ত আসল Transaction ID (trxID) দিয়ে আপডেট
            order.Payment.TransactionId = executeResult.TransactionId ?? request.PaymentId;
        }


        foreach (var item in order.OrderItems)
        {
            var product = await _productRepository.GetQueryable()
                .FirstOrDefaultAsync(p => p.Id == item.ProductId, cancellationToken);

            if (product != null)
            {

                if (product.Stock >= item.Quantity)
                {
                    product.Stock -= item.Quantity;
                }
                else
                {
                    product.Stock = 0;
                }
            }
        }


        await _orderRepository.SaveChangesAsync();
        await _productRepository.SaveChangesAsync();

        return Result.Success;
    }
}