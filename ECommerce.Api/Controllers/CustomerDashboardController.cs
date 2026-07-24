using System.Security.Claims;
using ECommerce.Application.Features.Dashboard.Queries.GetCustomerOrders;
using ECommerce.Application.DTOs.Dashboard;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    public async Task<IActionResult> GetMyOrders([FromQuery] Guid? userProfileId)
    {
        // ১. Query Parameter-এ UserProfileId না থাকলে JWT Claims থেকে বের করার চেষ্টা করবে
        var targetProfileId = userProfileId ?? GetUserProfileIdFromClaims();

        if (targetProfileId == Guid.Empty)
        {
            return Unauthorized(new { message = "Invalid or missing User Profile ID." });
        }

        // ২. Query পাঠানো
        var query = new GetCustomerOrdersQuery(targetProfileId);
        var result = await _mediator.Send(query);

        // ৩. ErrorOr Response Mapping
        return result.Match(
            orders => Ok(orders),
            errors => Problem(
                statusCode: GetStatusCode(errors.FirstOrDefault().Type),
                title: errors.FirstOrDefault().Description
            )
        );
    }

    private Guid GetUserProfileIdFromClaims()
    {
        // আপনার টোকেনে UserProfileId যে Claim-এ সেট করা আছে সেটি ব্যবহার করুন (e.g., "UserProfileId" or ClaimTypes.NameIdentifier)
        var userProfileIdClaim = User.FindFirst("UserProfileId")?.Value
                              ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return Guid.TryParse(userProfileIdClaim, out var profileId) ? profileId : Guid.Empty;
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