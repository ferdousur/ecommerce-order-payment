using ECommerce.Application.Features.Category.Command.CreateCategory;
using ECommerce.Application.Features.User.Command.RegisterUser;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/categories")]
[Authorize(Roles = "Admin")]
public class CategoryCreate : ControllerBase
{
    private readonly IMediator _mediator;
    public CategoryCreate(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> RegisterAsync([FromBody] CreateCategoryCommand command)
    {
        var result = await _mediator.Send(command);

        if (result.IsError)
        {
            return BadRequest(result.Errors);
        }

        return Ok(result.Value);
    }
}