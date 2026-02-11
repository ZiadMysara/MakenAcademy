using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace Maken.Api.Middleware;

/// <summary>
/// Global exception handling middleware that catches unhandled exceptions
/// and returns standardized error responses.
/// </summary>
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExceptionMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="environment">The host environment.</param>
    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    /// <summary>
    /// Invokes the middleware to handle exceptions.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/problem+json";
        
        var statusCode = HttpStatusCode.InternalServerError;
        var title = "An error occurred while processing your request.";
        var detail = "An unexpected error occurred. Please try again later.";
        var type = "https://tools.ietf.org/html/rfc7231#section-6.6.1";

        // Handle specific exception types
        if (exception is UnauthorizedAccessException)
        {
            // Treat tenant isolation violations as NotFound for security
            // (don't reveal that the resource exists in another tenant)
            statusCode = HttpStatusCode.NotFound;
            title = "Resource not found";
            detail = "The requested resource was not found.";
            type = "https://tools.ietf.org/html/rfc7231#section-6.5.4";
        }
        else if (exception is KeyNotFoundException)
        {
            statusCode = HttpStatusCode.NotFound;
            title = "Resource not found";
            detail = exception.Message;
            type = "https://tools.ietf.org/html/rfc7231#section-6.5.4";
        }
        else if (exception is ArgumentException or ArgumentNullException)
        {
            statusCode = HttpStatusCode.BadRequest;
            title = "Invalid request";
            detail = exception.Message;
            type = "https://tools.ietf.org/html/rfc7231#section-6.5.1";
        }
        else if (_environment.IsDevelopment())
        {
            // In development, include exception details
            detail = exception.Message;
        }

        context.Response.StatusCode = (int)statusCode;

        var problemDetails = new ProblemDetails
        {
            Status = (int)statusCode,
            Title = title,
            Detail = detail,
            Type = type,
            Instance = context.Request.Path
        };

        // Add correlation ID if available
        if (context.Items.TryGetValue("CorrelationId", out var correlationId))
        {
            problemDetails.Extensions["correlationId"] = correlationId;
        }

        // In development, add exception details as extensions
        if (_environment.IsDevelopment())
        {
            problemDetails.Extensions["exception"] = exception.GetType().Name;
            problemDetails.Extensions["stackTrace"] = exception.StackTrace ?? string.Empty;
        }

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(problemDetails, options);
        await context.Response.WriteAsync(json);
    }
}
