using Maken.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace Maken.Infrastructure.Persistence;

/// <summary>
/// Unit of Work implementation for managing database transactions.
/// Provides access to repositories and transaction management.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly MakenDbContext _context;
    private readonly Dictionary<Type, object> _repositories;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(MakenDbContext context)
    {
        _context = context;
        _repositories = new Dictionary<Type, object>();
    }

    /// <summary>
    /// Gets a repository for the specified entity type.
    /// Repositories are cached per unit of work instance.
    /// </summary>
    public IRepository<TEntity> Repository<TEntity>() where TEntity : class
    {
        var type = typeof(TEntity);

        if (!_repositories.ContainsKey(type))
        {
            var repositoryType = typeof(Repositories.Repository<>).MakeGenericType(type);
            var repositoryInstance = Activator.CreateInstance(repositoryType, _context);
            _repositories[type] = repositoryInstance!;
        }

        return (IRepository<TEntity>)_repositories[type];
    }

    /// <summary>
    /// Saves all changes made in this unit of work to the database.
    /// </summary>
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Begins a new database transaction.
    /// </summary>
    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    /// <summary>
    /// Commits the current transaction.
    /// </summary>
    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
        {
            throw new InvalidOperationException("No active transaction to commit.");
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            await _transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        finally
        {
            _transaction.Dispose();
            _transaction = null;
        }
    }

    /// <summary>
    /// Rolls back the current transaction.
    /// </summary>
    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync(cancellationToken);
            _transaction.Dispose();
            _transaction = null;
        }
    }

    /// <summary>
    /// Disposes the unit of work and any active transactions.
    /// </summary>
    public void Dispose()
    {
        _transaction?.Dispose();
        _repositories.Clear();
    }
}
