using Maken.Application.Common.Interfaces;

namespace Maken.Api.Middleware;

/// <summary>
/// Middleware for extracting subdomain and resolving tenant context.
/// Constitution requirement: Tenant must be resolved from subdomain (e.g., academy.maken.app).
/// User Story 2: Tenant Data Isolation (Priority P1)
/// </summary>
/// <remarks>
/// This middleware:
/// 1. Extracts subdomain from the Host header
/// 2. Resolves tenant ID using ITenantResolver
/// 3. Sets tenant context in HttpContext.Items for downstream use
/// 4. Returns 404 for unknown or reserved subdomains
/// 5. Allows requests without subdomain (root domain, localhost) for PlatformAdmin operations
/// </remarks>
public class TenantMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly HashSet<string> LocalhostHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "localhost",
        "127.0.0.1",
        "::1"
    };

    private static readonly HashSet<string> SkipPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/health",
        "/api/v1/health"
    };

    public TenantMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Processes the HTTP request and resolves tenant context.
    /// T045: Subdomain extraction and tenant resolution
    /// </summary>
    public async Task InvokeAsync(HttpContext context, ITenantResolver tenantResolver)
    {
        // Skip tenant resolution for certain paths (health checks, etc.)
        if (ShouldSkipTenantResolution(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var host = context.Request.Host.Host;

        // Allow localhost and IP addresses (development)
        if (IsLocalhost(host))
        {
            await _next(context);
            return;
        }

        // Extract subdomain from host
        var subdomain = ExtractSubdomain(host);

        // If no subdomain (root domain), allow request to proceed without tenant context
        // This is for PlatformAdmin operations
        if (string.IsNullOrEmpty(subdomain))
        {
            await _next(context);
            return;
        }

        // Check if subdomain is reserved - return 404 immediately without calling resolver
        if (tenantResolver.IsReservedSubdomain(subdomain))
        {
            context.Response.StatusCode = 404;
            await context.Response.WriteAsync("Tenant not found");
            return;
        }

        // Resolve tenant ID from subdomain
        var tenantId = await tenantResolver.ResolveAsync(subdomain, context.RequestAborted);

        // If tenant not found, return 404
        if (tenantId == null)
        {
            context.Response.StatusCode = 404;
            await context.Response.WriteAsync("Tenant not found");
            return;
        }

        // Set tenant context in HttpContext.Items for downstream use
        context.Items["TenantId"] = tenantId.Value;
        context.Items["Subdomain"] = subdomain;

        // Continue to next middleware
        await _next(context);
    }

    /// <summary>
    /// Determines if tenant resolution should be skipped for the given path.
    /// </summary>
    private static bool ShouldSkipTenantResolution(PathString path)
    {
        return SkipPaths.Contains(path.Value ?? string.Empty);
    }

    /// <summary>
    /// Extracts subdomain from host.
    /// Examples:
    /// - "academy.maken.app" → "academy"
    /// - "maken.app" → null (root domain)
    /// - "localhost" → null
    /// </summary>
    private static string? ExtractSubdomain(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            return null;
        }

        // Remove port if present
        var hostWithoutPort = host.Split(':')[0];

        // Split by dots
        var parts = hostWithoutPort.Split('.');

        // If less than 3 parts, no subdomain (e.g., "maken.app" or "localhost")
        if (parts.Length < 3)
        {
            return null;
        }

        // First part is the subdomain
        return parts[0];
    }

    /// <summary>
    /// Checks if the host is localhost or an IP address.
    /// </summary>
    private static bool IsLocalhost(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            return false;
        }

        // Remove port if present
        var hostWithoutPort = host.Split(':')[0];

        return LocalhostHosts.Contains(hostWithoutPort);
    }
}
