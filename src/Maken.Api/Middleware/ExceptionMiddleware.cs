using System.Net;
using System.Text.Json;
using Maken.Api.Models;

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
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var errorCode = "INTERNAL_SERVER_ERROR";
        var errorMessage = "An unexpected error occurred. Please try again later.";
        Dictionary<string, object>? details = null;

        // In development, include exception details
        if (_environment.IsDevelopment())
        {
            errorMessage = exception.Message;
            details = new Dictionary<string, object>
            {
                ["exception"] = exception.GetType().Name,
                ["stackTrace"] = exception.StackTrace ?? string.Empty
            };
        }

        var response = ApiResponse<object>.Fail(errorCode, errorMessage, details);

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(response, options);
        await context.Response.WriteAsync(json);
    }
}
