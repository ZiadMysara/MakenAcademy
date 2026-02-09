namespace Maken.Domain.Common;

/// <summary>
/// Abstract base class for entities that belong to a specific tenant.
/// Extends BaseEntity and implements ITenantScoped.
/// </summary>
/// <remarks>
/// Use this class for all entities that are tenant-specific (e.g., User, Course, Lesson).
/// TenantId is set at construction and cannot be changed (tenant ownership is immutable).
/// Global query filters will automatically scope queries by TenantId.
/// </remarks>
public abstract class TenantScopedEntity : BaseEntity, ITenantScoped
{
    /// <summary>
    /// The ID of the tenant that owns this entity.
    /// Set at construction and immutable - entities cannot change tenant ownership.
    /// </summary>
    public Guid TenantId { get; private set; }

    /// <summary>
    /// Initializes a new tenant-scoped entity.
    /// </summary>
    /// <param name="tenantId">The ID of the tenant that owns this entity.</param>
    /// <exception cref="ArgumentException">Thrown when tenantId is empty.</exception>
    protected TenantScopedEntity(Guid tenantId) : base()
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId cannot be empty.", nameof(tenantId));
        }

        TenantId = tenantId;
    }
}
