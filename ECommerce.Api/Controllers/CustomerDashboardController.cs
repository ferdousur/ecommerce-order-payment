using ECommerce.Application.Features.Dashboard.Queries.GetCustomerOrders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;

namespace ECommerce.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Customer")]
public class CustomerDashboardController : ControllerBase
{
    private readonly ISender _mediator;

    public CustomerDashboardController(ISender mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Logged-in Customer-এর সব অর্ডার ও পেমেন্ট হিস্টোরি রিটার্ন করে
    /// </summary>
    [HttpGet("orders")]
    public async Task<IActionResult> GetMyOrders()
    {
        // যেহেতু টোকেন থেকে আইডি হ্যান্ডলার নিজে নিয়ে নেবে, তাই এখানে শুধু এম্টি কুয়েরি পাঠালেই হবে
        var query = new GetCustomerOrdersQuery();
        var result = await _mediator.Send(query);

        return result.Match(
            orders => Ok(orders),
            errors => Problem(
                statusCode: GetStatusCode(errors.FirstOrDefault().Type),
                title: errors.FirstOrDefault().Description
            )
        );
    }

    private static int GetStatusCode(ErrorOr.ErrorType errorType) => errorType switch
    {
        ErrorOr.ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorOr.ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorOr.ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorOr.ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        _ => StatusCodes.Status500InternalServerError
    };
}