using ECommerce.Application.Cores.Abstractions;
using ECommerce.Application.DTOs.Dashboard;
using ErrorOr;

namespace ECommerce.Application.Features.Dashboard.Queries.GetCustomerOrders;

public record GetCustomerOrdersQuery(Guid UserProfileId)
    : IQuery<ErrorOr<List<CustomerDashboardOrderDto>>>;