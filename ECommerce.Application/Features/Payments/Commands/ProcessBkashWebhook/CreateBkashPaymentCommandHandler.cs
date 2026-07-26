using ECommerce.Application.Interfaces;
using ECommerce.Application.Payments.DTOs;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Features.Payments.Commands.CreateBkashPayment;

public class CreateBkashPaymentCommandHandler : IRequestHandler<CreateBkashPaymentCommand, ErrorOr<PaymentResult>>
{
    private readonly IEnumerable<IPaymentProcessor> _paymentProcessors;
    private readonly IRepository<Order> _orderRepository;
    private readonly ICurrencyConverterService _currencyConverterService;

    public CreateBkashPaymentCommandHandler(
        IEnumerable<IPaymentProcessor> paymentProcessors,
        IRepository<Order> orderRepository,
        ICurrencyConverterService currencyConverterService)
    {
        _paymentProcessors = paymentProcessors;
        _orderRepository = orderRepository;
        _currencyConverterService = currencyConverterService;
    }

    public async Task<ErrorOr<PaymentResult>> Handle(CreateBkashPaymentCommand request, CancellationToken cancellationToken)
    {
        var processor = _paymentProcessors.FirstOrDefault(p => p.Provider == PaymentProvider.Bkash);
        if (processor == null)
        {
            return Error.NotFound("Payment.ProcessorNotFound", "bKash processor is not configured.");
        }


        var order = await _orderRepository.GetQueryable()
            .Include(o => o.Payment)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

        if (order == null)
        {
            return Error.NotFound("Order.NotFound", $"Order with ID {request.OrderId} was not found.");
        }

        if (order.Status == OrderStatus.Paid || (order.Payment != null && order.Payment.Status == PaymentStatus.Success))
        {
            return Error.Conflict("Order.AlreadyPaid", "This order has already been paid for.");
        }


        decimal amountInUsd = order.TotalAmount;
        decimal amountInBdt = await _currencyConverterService.ConvertUsdToBdtAsync(amountInUsd);

        var paymentRequest = new PaymentRequest(
            OrderId: order.Id,
            Amount: order.TotalAmount,
            Currency: "BDT"
        );

        var result = await processor.ProcessPaymentAsync(paymentRequest);
        if (!result.IsSuccess)
        {
            return Error.Failure("Payment.BkashCreationFailed", result.ErrorMessage ?? "Failed to create bKash payment session.");
        }


        if (order.Payment == null)
        {
            order.Payment = new Payment
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                Provider = PaymentProvider.Bkash,
                Status = PaymentStatus.Pending,
                TransactionId = result.TransactionId
            };
        }
        else
        {
            order.Payment.Provider = PaymentProvider.Bkash;
            order.Payment.Status = PaymentStatus.Pending;
            order.Payment.TransactionId = result.TransactionId;
        }

        await _orderRepository.SaveChangesAsync();

        return result;
    }
}