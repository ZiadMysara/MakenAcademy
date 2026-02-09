namespace Maken.Domain.Common;

/// <summary>
/// Interface for entities that belong to a specific tenant.
/// Constitution requirement: All tenant-scoped entities must implement this interface
/// to enable automatic tenant isolation via EF Core global query filters.
/// </summary>
/// <remarks>
/// Entities implementing this interface will have queries automatically scoped by TenantId.
/// Global query filter: WHERE TenantId = @currentTenantId AND IsDeleted = false
/// </remarks>
public interface ITenantScoped
{
    /// <summary>
    /// The ID of the tenant that owns this entity.
    /// Set at construction and never changes (immutable after creation).
    /// </summary>
    Guid TenantId { get; }
}
