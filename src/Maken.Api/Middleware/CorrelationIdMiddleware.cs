namespace Maken.Api.Middleware;

/// <summary>
/// Middleware that adds a correlation ID to each request for tracking and logging purposes.
/// </summary>
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string CorrelationIdHeader = "X-Correlation-ID";

    /// <summary>
    /// Initializes a new instance of the <see cref="CorrelationIdMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Invokes the middleware to add correlation ID to the request and response.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        // Check if correlation ID already exists in request headers
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault();

        // If not provided, generate a new one
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = Guid.NewGuid().ToString();
        }

        // Add correlation ID to response headers
        context.Response.Headers.Append(CorrelationIdHeader, correlationId);

        // Store correlation ID in HttpContext items for access by other middleware/controllers
        context.Items["CorrelationId"] = correlationId;

        await _next(context);
    }
}
