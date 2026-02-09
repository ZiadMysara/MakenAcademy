using Maken.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Maken.Infrastructure.Services;

/// <summary>
/// Implementation of ITenantContext that resolves tenant information from HttpContext.
/// Constitution requirement: Tenant is resolved from subdomain (e.g., academy.maken.app).
/// </summary>
/// <remarks>
/// This is a scoped service that extracts tenant information from the current HTTP request.
/// The actual tenant resolution (subdomain → TenantId lookup) will be implemented in Phase 4 (User Story 2).
/// For now, this provides the infrastructure for tenant context management.
/// </remarks>
public class TenantContext : ITenantContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Gets the current tenant ID from HttpContext items.
    /// Set by TenantMiddleware (to be implemented in Phase 4).
    /// </summary>
    public Guid? TenantId
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                return null;
            }

            // Check if tenant ID was set by middleware
            if (httpContext.Items.TryGetValue("TenantId", out var tenantIdObj) && tenantIdObj is Guid tenantId)
            {
                return tenantId;
            }

            return null;
        }
    }

    /// <summary>
    /// Gets the subdomain from the current request.
    /// </summary>
    public string? Subdomain
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                return null;
            }

            // Check if subdomain was set by middleware
            if (httpContext.Items.TryGetValue("Subdomain", out var subdomainObj) && subdomainObj is string subdomain)
            {
                return subdomain;
            }

            return null;
        }
    }

    /// <summary>
    /// Indicates whether a tenant context is available.
    /// </summary>
    public bool HasTenant => TenantId.HasValue;
}
