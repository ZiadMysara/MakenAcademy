namespace Maken.Application.Common.Interfaces;

/// <summary>
/// Service for resolving tenant identity from subdomain.
/// Constitution requirement: Tenant must be resolved from subdomain (e.g., academy.maken.app).
/// User Story 2: Tenant Data Isolation (Priority P1)
/// </summary>
/// <remarks>
/// This interface defines the contract for tenant resolution with caching support.
/// Implementation should include:
/// - Subdomain lookup in database
/// - Memory cache with 5-minute TTL (per research.md)
/// - Reserved subdomain validation (www, api, admin, app)
/// </remarks>
public interface ITenantResolver
{
    /// <summary>
    /// Resolves a tenant ID from the given subdomain.
    /// </summary>
    /// <param name="subdomain">The subdomain to resolve (e.g., "academy" from "academy.maken.app")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>
    /// The tenant ID if found, null if:
    /// - Subdomain is null or empty
    /// - Subdomain is reserved (www, api, admin, app)
    /// - Tenant not found in database
    /// </returns>
    Task<Guid?> ResolveAsync(string? subdomain, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a subdomain is reserved and cannot be used for tenant resolution.
    /// Reserved subdomains: www, api, admin, app
    /// </summary>
    /// <param name="subdomain">The subdomain to check</param>
    /// <returns>True if the subdomain is reserved, false otherwise</returns>
    bool IsReservedSubdomain(string? subdomain);
}
