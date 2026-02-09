namespace Maken.Application.Common.Interfaces;

/// <summary>
/// Provides access to the current tenant context for the request.
/// Constitution requirement: All queries must be automatically scoped by TenantId.
/// </summary>
/// <remarks>
/// This interface is implemented in the Infrastructure layer and resolves the tenant
/// from the HTTP request (subdomain extraction). The resolved tenant is used by
/// EF Core global query filters to automatically scope all queries.
/// </remarks>
public interface ITenantContext
{
    /// <summary>
    /// Gets the ID of the current tenant for this request.
    /// Returns null for PlatformAdmin operations that don't require tenant context.
    /// </summary>
    Guid? TenantId { get; }

    /// <summary>
    /// Gets the subdomain of the current tenant for this request.
    /// Used for tenant resolution from the Host header.
    /// </summary>
    string? Subdomain { get; }

    /// <summary>
    /// Indicates whether a tenant context has been resolved for this request.
    /// </summary>
    bool HasTenant { get; }
}
