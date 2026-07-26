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
    private readonly ICurrentUserService _currentUserService;

    public GetCustomerOrdersQueryHandler(
        IRepository<Domain.Entities.Order> orderRepository,
        ICurrentUserService currentUserService)
    {
        _orderRepository = orderRepository;
        _currentUserService = currentUserService;
    }

    public async Task<ErrorOr<List<CustomerDashboardOrderDto>>> Handle(
        GetCustomerOrdersQuery request,
        CancellationToken cancellationToken)
    {
        // Token থেকে ইউজার আইডি নেওয়া
        var currentUser = _currentUserService.UserId;
        if (currentUser is null || currentUser == Guid.Empty)
        {
            return Error.Unauthorized("User.Unauthorized", "User is not authenticated or token is invalid.");
        }

        var orderDtos = await _orderRepository.GetQueryable()
            .AsNoTracking()
            .Where(o => o.UserProfileId == currentUser.Value)
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