using System.Linq.Expressions;
using Maken.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Maken.Infrastructure.Persistence.Repositories;

/// <summary>
/// Generic repository implementation for data access operations.
/// Provides common CRUD operations for all entities.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
public class Repository<TEntity> : IRepository<TEntity> where TEntity : class
{
    private readonly MakenDbContext _context;
    protected readonly DbSet<TEntity> _dbSet;

    public Repository(MakenDbContext context)
    {
        _context = context;
        _dbSet = context.Set<TEntity>();
    }

    public async Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // IMPORTANT: We use IgnoreQueryFilters() here because command handlers need to retrieve
        // entities to check tenant ownership BEFORE throwing UnauthorizedAccessException.
        // If we don't ignore filters, the entity won't be found and we'll return 404 instead of 401/403.
        // Command handlers are responsible for checking tenant ownership after retrieval.
        var entity = await _dbSet.IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == id, cancellationToken);
        
        // Apply soft delete filter manually - we never want to return soft-deleted entities
        if (entity != null && typeof(TEntity).GetProperty("IsDeleted") != null)
        {
            var isDeleted = (bool)typeof(TEntity).GetProperty("IsDeleted")!.GetValue(entity)!;
            if (isDeleted)
            {
                return null;
            }
        }
        
        return entity;
    }

    public async Task<List<TEntity>> GetAllAsync(
        Expression<Func<TEntity, bool>>? filter = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<TEntity> query = _dbSet;

        if (filter != null)
        {
            query = query.Where(filter);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<TEntity?> GetFirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> filter,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(filter, cancellationToken);
    }

    public async Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> filter,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(filter, cancellationToken);
    }

    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(entity, cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddRangeAsync(entities, cancellationToken);
    }

    public void Update(TEntity entity)
    {
        _dbSet.Update(entity);
    }

    public void Remove(TEntity entity)
    {
        _dbSet.Remove(entity);
    }

    public void RemoveRange(IEnumerable<TEntity> entities)
    {
        _dbSet.RemoveRange(entities);
    }
}
