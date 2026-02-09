namespace Maken.Domain.Common;

/// <summary>
/// Base entity for all domain entities in the Maken platform.
/// Enforces Constitution requirements:
/// - UUID primary keys (server-generated only)
/// - Soft delete support (no physical deletes allowed)
/// - Audit fields for tracking changes
/// </summary>
/// <remarks>
/// All entities MUST inherit from this class or TenantScopedEntity.
/// IDs are generated server-side in the constructor and cannot be changed.
/// Soft delete is enforced - use SoftDelete() method instead of removing entities.
/// </remarks>
public abstract class BaseEntity
{
    /// <summary>
    /// Unique identifier for this entity.
    /// Generated server-side using Guid.NewGuid() - never accept client-provided IDs.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// UTC timestamp when this entity was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// User ID who created this entity (optional).
    /// </summary>
    public Guid? CreatedBy { get; private set; }

    /// <summary>
    /// UTC timestamp when this entity was last updated (optional).
    /// </summary>
    public DateTime? UpdatedAt { get; private set; }

    /// <summary>
    /// User ID who last updated this entity (optional).
    /// </summary>
    public Guid? UpdatedBy { get; private set; }

    /// <summary>
    /// Soft delete flag. When true, entity is logically deleted but remains in database.
    /// Constitution requirement: No physical DELETE operations allowed.
    /// </summary>
    public bool IsDeleted { get; private set; }

    /// <summary>
    /// UTC timestamp when this entity was soft deleted (optional).
    /// </summary>
    public DateTime? DeletedAt { get; private set; }

    /// <summary>
    /// User ID who soft deleted this entity (optional).
    /// </summary>
    public Guid? DeletedBy { get; private set; }

    /// <summary>
    /// Initializes a new instance of BaseEntity with a server-generated UUID.
    /// </summary>
    protected BaseEntity()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        IsDeleted = false;
    }

    /// <summary>
    /// Marks this entity as created by a specific user.
    /// </summary>
    /// <param name="userId">The ID of the user who created this entity.</param>
    public void SetCreatedBy(Guid userId)
    {
        CreatedBy = userId;
    }

    /// <summary>
    /// Marks this entity as updated by a specific user.
    /// </summary>
    /// <param name="userId">The ID of the user who updated this entity.</param>
    public void SetUpdatedBy(Guid userId)
    {
        UpdatedBy = userId;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Performs a soft delete on this entity.
    /// Constitution requirement: No physical deletes - use soft delete instead.
    /// </summary>
    /// <param name="userId">The ID of the user performing the delete (optional).</param>
    public void SoftDelete(Guid? userId = null)
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = userId;
    }

    /// <summary>
    /// Restores a soft-deleted entity.
    /// </summary>
    public void Restore()
    {
        IsDeleted = false;
        DeletedAt = null;
        DeletedBy = null;
    }
}
