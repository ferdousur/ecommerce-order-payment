using ECommerce.Application.Features.Category.Command.CreateProduct;
using ECommerce.Application.Features.Product.Command.DeleteProduct;
using ECommerce.Application.Features.Product.Command.UpdateProduct;
using ECommerce.Application.Features.Product.Query.GetAllProducts;
using ECommerce.Application.Features.Product.Query.GetProductById;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;

namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/products")]
[Authorize(Roles = "Admin")]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProductsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductCommand command)
    {
        var result = await _mediator.Send(command);

        if (result.IsError)
        {
            return BadRequest(result.Errors);
        }

        return Ok(result.Value);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var command = new DeleteProductCommand(id);
        var result = await _mediator.Send(command);

        if (result.IsError)
        {
            return BadRequest(result.Errors);
        }

        return NoContent();
    }


    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductCommand command)
    {
        if (id != command.Id)
        {
            return BadRequest("Route Id and Body Id mismatch.");
        }

        var result = await _mediator.Send(command);

        if (result.IsError)
        {
            return BadRequest(result.Errors);
        }

        return Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetProductByIdQuery(id));

        if (result.IsError)
        {
            return NotFound(result.Errors);
        }

        return Ok(result.Value);
    }


    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll()
    {
        var result = await _mediator.Send(new GetAllProductsQuery());

        if (result.IsError)
        {
            return BadRequest(result.Errors);
        }

        return Ok(result.Value);
    }
}