using System.Linq.Expressions;

namespace Maken.Application.Common.Interfaces;

/// <summary>
/// Generic repository interface for data access operations.
/// Provides common CRUD operations for all entities.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <remarks>
/// Constitution requirements enforced:
/// - All queries are automatically scoped by TenantId (via EF Core global filters)
/// - Delete operations perform soft delete (no physical deletes)
/// - UUID primary keys are used for all entities
/// </remarks>
public interface IRepository<TEntity> where TEntity : class
{
    /// <summary>
    /// Gets an entity by its ID.
    /// </summary>
    /// <param name="id">The entity ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The entity if found, otherwise null.</returns>
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all entities matching the optional filter.
    /// </summary>
    /// <param name="filter">Optional filter expression.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of entities.</returns>
    Task<List<TEntity>> GetAllAsync(
        Expression<Func<TEntity, bool>>? filter = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the first entity matching the filter.
    /// </summary>
    /// <param name="filter">Filter expression.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The first entity if found, otherwise null.</returns>
    Task<TEntity?> GetFirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> filter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if any entity matches the filter.
    /// </summary>
    /// <param name="filter">Filter expression.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if any entity matches, otherwise false.</returns>
    Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> filter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new entity.
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds multiple entities.
    /// </summary>
    /// <param name="entities">The entities to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing entity.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    void Update(TEntity entity);

    /// <summary>
    /// Removes an entity (soft delete).
    /// Constitution requirement: No physical deletes allowed.
    /// </summary>
    /// <param name="entity">The entity to remove.</param>
    void Remove(TEntity entity);

    /// <summary>
    /// Removes multiple entities (soft delete).
    /// Constitution requirement: No physical deletes allowed.
    /// </summary>
    /// <param name="entities">The entities to remove.</param>
    void RemoveRange(IEnumerable<TEntity> entities);
}
