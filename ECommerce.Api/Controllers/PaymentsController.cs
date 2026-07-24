using System.Text.Json;
using ECommerce.Application.Features.Payments.Commands.ProcessStripeWebhook;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly ISender _sender;

    public PaymentsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> StripeWebhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var signatureHeader = Request.Headers["Stripe-Signature"].ToString();

        var command = new ProcessStripeWebhookCommand(json, signatureHeader);
        var result = await _sender.Send(command);

        return result.Match<IActionResult>(
            _ => Ok(),
            errors => BadRequest(errors.FirstOrDefault().Description)
        );  
    }
}