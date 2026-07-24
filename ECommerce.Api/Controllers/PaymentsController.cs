using System.Text.Json;
using ECommerce.Application.Features.Payments.Commands.CreateBkashPayment;
using ECommerce.Application.Features.Payments.Commands.ExecuteBkashPayment;
using ECommerce.Application.Features.Payments.Commands.ProcessStripeWebhook;
using ErrorOr;
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

    /// <summary>
    /// Stripe Webhook Handler
    /// </summary>
    [HttpPost("webhook")]
    public async Task<IActionResult> StripeWebhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var signatureHeader = Request.Headers["Stripe-Signature"].ToString();

        var command = new ProcessStripeWebhookCommand(json, signatureHeader);
        var result = await _sender.Send(command);

        return result.Match<IActionResult>(
            _ => Ok(),
            errors => Problem(errors)
        );
    }

    /// <summary>
    /// bKash Payment Create Point
    /// </summary>
    [HttpPost("bkash/create")]
    public async Task<IActionResult> CreateBkashPayment(
        [FromBody] CreateBkashPaymentCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);

        return result.Match(
            paymentResult => Ok(paymentResult),
            errors => Problem(errors)
        );
    }

    /// <summary>
    /// bKash পেমেন্ট কনফার্মেশন / Execute পয়েন্ট
    /// bKash ইউজারকে OTP/PIN দেওয়ার পর এই Callback URL-এ পাঠায়
    /// </summary>
    [HttpGet("bkash/execute")]
    public async Task<IActionResult> BkashCallback(
        [FromQuery] string paymentID,
        [FromQuery] string status,
        CancellationToken cancellationToken)
    {
        // ১. ইউজার ক্যানসেল বা ফেইল করলে
        if (status == "cancel" || status == "failure")
        {
            return BadRequest(new { Message = $"Payment was {status}ed by user." });
        }

        // ২. পেমেন্ট সাকসেস হলে MediatR-এর মাধ্যমে ExecuteBkashPaymentCommand রান হবে
        var command = new ExecuteBkashPaymentCommand(paymentID);
        var result = await _sender.Send(command, cancellationToken);

        return result.Match(
            success => Ok(new
            {
                Success = true,
                Message = "Payment completed successfully!",
                PaymentId = paymentID
            }),
            errors => Problem(errors)
        );
    }

    /// <summary>
    /// ErrorOr এররগুলোর জন্য Helper Method
    /// </summary>
    private IActionResult Problem(List<Error> errors)
    {
        if (errors.Count == 0) return Problem();

        var firstError = errors[0];
        var statusCode = firstError.Type switch
        {
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        return Problem(
            statusCode: statusCode,
            title: firstError.Code,
            detail: firstError.Description
        );
    }
}