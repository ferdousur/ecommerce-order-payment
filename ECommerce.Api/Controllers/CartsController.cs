using ECommerce.Application.Features.Cart.Commands.AddToCart;
using ECommerce.Application.Features.Cart.Queries.GetMyCart;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/carts")]
[Authorize(Roles = "Customer")]
public class CartsController : ControllerBase
{
    private readonly IMediator _mediator;

    public CartsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{userProfileId:guid}")]
    public async Task<IActionResult> GetMyCart(Guid userProfileId)
    {
        var result = await _mediator.Send(new GetMyCartQuery(userProfileId));

        if (result.IsError)
        {
            return NotFound(result.Errors);
        }

        return Ok(result.Value);
    }

    [HttpPost("add")]
    public async Task<IActionResult> AddToCart([FromBody] AddToCartCommand command)
    {
        var result = await _mediator.Send(command);

        if (result.IsError)
        {
            return BadRequest(result.Errors);
        }

        return Ok(result.Value);
    }
}