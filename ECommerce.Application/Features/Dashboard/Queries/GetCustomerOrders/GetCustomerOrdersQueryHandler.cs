using ECommerce.Application.Cores.Abstractions;
using ECommerce.Application.DTOs.Dashboard;
using ECommerce.Application.Interfaces;
using ErrorOr;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Features.Dashboard.Queries.GetCustomerOrders;

public class GetCustomerOrdersQueryHandler
    : IQueryHandler<GetCustomerOrdersQuery, ErrorOr<List<CustomerDashboardOrderDto>>>
{
    private readonly IRepository<Domain.Entities.Order> _orderRepository;

    public GetCustomerOrdersQueryHandler(IRepository<Domain.Entities.Order> orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<ErrorOr<List<CustomerDashboardOrderDto>>> Handle(
        GetCustomerOrdersQuery request,
        CancellationToken cancellationToken)
    {
        var orderDtos = await _orderRepository.GetQueryable()
            .AsNoTracking()
            .Where(o => o.UserProfileId == request.UserProfileId)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new CustomerDashboardOrderDto
            {
                OrderId = o.Id,
                OrderDate = o.CreatedAt,
                TotalAmount = o.TotalAmount,
                OrderStatus = o.Status.ToString(),
                ShippingAddress = o.ShippingAddress,
                TotalItems = o.OrderItems.Count,

                Payment = o.Payment != null ? new CustomerPaymentInfoDto
                {
                    PaymentId = o.Payment.Id,
                    Provider = o.Payment.Provider.ToString(),
                    PaymentStatus = o.Payment.Status.ToString(),
                    TransactionId = o.Payment.TransactionId,
                    PaymentDate = o.Payment.CreatedAt
                } : null
            })
            .ToListAsync(cancellationToken);

        return orderDtos;
    }
}