using Maken.Application.Common.Interfaces;
using Maken.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Maken.Infrastructure.Services;

/// <summary>
/// Implementation of ITenantResolver with subdomain lookup and caching.
/// Constitution requirement: Tenant resolution must complete within 50ms (NFR-004).
/// User Story 2: Tenant Data Isolation (Priority P1)
/// </summary>
/// <remarks>
/// This service resolves tenant IDs from subdomains with the following features:
/// - Database lookup by subdomain
/// - Memory cache with 5-minute TTL (per research.md)
/// - Reserved subdomain validation (www, api, admin, app)
/// - Case-insensitive subdomain matching
/// </remarks>
public class TenantResolver : ITenantResolver
{
    private readonly MakenDbContext _context;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
    private static readonly HashSet<string> ReservedSubdomains = new(StringComparer.OrdinalIgnoreCase)
    {
        "www",
        "api",
        "admin",
        "app"
    };

    public TenantResolver(MakenDbContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    /// <summary>
    /// Resolves a tenant ID from the given subdomain with caching.
    /// T044: Subdomain lookup with cache
    /// T048: Reserved subdomain validation
    /// T049: Memory cache implementation (5 min TTL)
    /// </summary>
    public async Task<Guid?> ResolveAsync(string? subdomain, CancellationToken cancellationToken = default)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(subdomain))
        {
            return null;
        }

        // Normalize subdomain (lowercase, trim)
        subdomain = subdomain.Trim().ToLowerInvariant();

        // Check if subdomain is reserved
        if (IsReservedSubdomain(subdomain))
        {
            return null;
        }

        // Try to get from cache first
        var cacheKey = $"tenant:subdomain:{subdomain}";
        if (_cache.TryGetValue<Guid?>(cacheKey, out var cachedTenantId))
        {
            return cachedTenantId;
        }

        // Query database for tenant
        // Note: We need to bypass tenant scoping for this query since we're resolving the tenant itself
        var tenant = await _context.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Subdomain == subdomain, cancellationToken);

        var tenantId = tenant?.Id;

        // Cache the result (including null results to avoid repeated DB queries for invalid subdomains)
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CacheDuration,
            Priority = CacheItemPriority.High
        };

        _cache.Set(cacheKey, tenantId, cacheOptions);

        return tenantId;
    }

    /// <summary>
    /// Checks if a subdomain is reserved.
    /// T048: Reserved subdomain validation
    /// </summary>
    public bool IsReservedSubdomain(string? subdomain)
    {
        if (string.IsNullOrWhiteSpace(subdomain))
        {
            return false;
        }

        return ReservedSubdomains.Contains(subdomain.Trim());
    }
}
