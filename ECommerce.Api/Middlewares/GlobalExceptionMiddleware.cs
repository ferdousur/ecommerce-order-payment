using System.Net;
using System.Text.Json;

namespace ECommerce.Api.Middlewares;

/// <summary>
/// Global middleware to intercept and handle unhandled exceptions across the application pipeline.
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            // Log the unhandled exception details to console and underlying logging providers
            _logger.LogError(ex, "[Global Exception] An unhandled exception occurred: {Message}", ex.Message);

            // Handle the response and write structured JSON output to client
            await HandleExceptionAsync(context, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        // Map domain or system exceptions to appropriate HTTP status codes
        var statusCode = exception switch
        {
            UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
            KeyNotFoundException => (int)HttpStatusCode.NotFound,
            ArgumentException => (int)HttpStatusCode.BadRequest,
            _ => (int)HttpStatusCode.InternalServerError
        };

        context.Response.StatusCode = statusCode;

        // Structured error response DTO for API clients
        var errorResponse = new
        {
            StatusCode = statusCode,
            Message = exception.Message,
            // Include stack trace only in non-production development environment for security
            Details = _env.IsDevelopment() ? exception.StackTrace?.ToString() : null,
            Timestamp = DateTime.UtcNow
        };

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var jsonResponse = JsonSerializer.Serialize(errorResponse, options);

        return context.Response.WriteAsync(jsonResponse);
    }
}