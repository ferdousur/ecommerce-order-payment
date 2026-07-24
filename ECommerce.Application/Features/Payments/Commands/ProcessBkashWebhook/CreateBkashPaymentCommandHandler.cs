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

    public CreateBkashPaymentCommandHandler(
        IEnumerable<IPaymentProcessor> paymentProcessors,
        IRepository<Order> orderRepository)
    {
        _paymentProcessors = paymentProcessors;
        _orderRepository = orderRepository;
    }

    public async Task<ErrorOr<PaymentResult>> Handle(CreateBkashPaymentCommand request, CancellationToken cancellationToken)
    {
        // ১. bKash Processor চেক
        var processor = _paymentProcessors.FirstOrDefault(p => p.Provider == PaymentProvider.Bkash);
        if (processor == null)
        {
            return Error.NotFound(
                code: "Payment.ProcessorNotFound",
                description: "bKash payment processor is not configured or registered."
            );
        }

        // ২. ডাটাবেসে Order আছে কি না চেক
        var order = await _orderRepository.GetQueryable()
            .Include(o => o.Payment)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

        if (order == null)
        {
            return Error.NotFound(
                code: "Order.NotFound",
                description: $"Order with ID {request.OrderId} was not found."
            );
        }

        // ৩. Order ইতোমধ্যে Paid কি না চেক (Double Payment প্রতিরোধ)
        if (order.Status == OrderStatus.Paid || (order.Payment != null && order.Payment.Status == PaymentStatus.Success))
        {
            return Error.Conflict(
                code: "Order.AlreadyPaid",
                description: "This order has already been paid for."
            );
        }

        // ৪. bKash API-তে Create Payment রিকোয়েস্ট পাঠানো
        var paymentRequest = new PaymentRequest(
            OrderId: order.Id,
            Amount: request.Amount > 0 ? request.Amount : order.TotalAmount,
            Currency: "BDT"
        );

        var result = await processor.ProcessPaymentAsync(paymentRequest);

        if (!result.IsSuccess)
        {
            return Error.Failure(
                code: "Payment.BkashCreationFailed",
                description: result.ErrorMessage ?? "Failed to create bKash payment session."
            );
        }

        // 🔑 ৫. সবচেয়ে গুরুত্বপূর্ণ: bKash-এর PaymentID ডাটাবেসে সেভ করা
        if (order.Payment == null)
        {
            order.Payment = new Payment
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                Provider = PaymentProvider.Bkash,
                Status = PaymentStatus.Pending,
                TransactionId = result.TransactionId // TR0011c... সেভ হবে
            };
        }
        else
        {
            order.Payment.Provider = PaymentProvider.Bkash;
            order.Payment.Status = PaymentStatus.Pending;
            order.Payment.TransactionId = result.TransactionId; // আপডেট করা হলো
        }

        await _orderRepository.SaveChangesAsync();

        return result;
    }
}