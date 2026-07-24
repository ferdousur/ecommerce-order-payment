using ECommerce.Application.Features.Orders.Commands.Checkout;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Customer")]
public class OrdersController : ControllerBase
{
    private readonly ISender _sender;

    public OrdersController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout([FromBody] CheckoutCommand command)
    {
        var result = await _sender.Send(command);

        return result.Match(
            response => Ok(response),
            errors => Problem(errors.FirstOrDefault().Description)
        );
    }
}