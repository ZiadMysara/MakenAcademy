using Maken.Application.Common.Interfaces;
using Maken.Domain.Common;
using Maken.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Maken.Infrastructure.Persistence;

/// <summary>
/// Entity Framework Core database context for the Maken platform.
/// Implements Constitution requirements:
/// - Global query filters for tenant isolation
/// - Global query filters for soft delete
/// - Automatic soft delete interception
/// </summary>
public class MakenDbContext : DbContext
{
    private readonly ITenantContext _tenantContext;

    public MakenDbContext(
        DbContextOptions<MakenDbContext> options,
        ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    // DbSets
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply entity configurations from the same assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MakenDbContext).Assembly);

        // Apply global query filters
        ApplyGlobalFilters(modelBuilder);
    }

    /// <summary>
    /// Applies global query filters for tenant isolation and soft delete.
    /// Constitution requirements:
    /// - All tenant-scoped queries automatically filtered by TenantId
    /// - All queries automatically exclude soft-deleted records
    /// </summary>
    private void ApplyGlobalFilters(ModelBuilder modelBuilder)
    {
        // Get all entity types that implement ITenantScoped
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;

            // Apply soft delete filter to all entities inheriting from BaseEntity
            if (typeof(BaseEntity).IsAssignableFrom(clrType))
            {
                var method = typeof(MakenDbContext)
                    .GetMethod(nameof(ApplySoftDeleteFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                    ?.MakeGenericMethod(clrType);

                method?.Invoke(null, new object[] { modelBuilder });
            }

            // Apply tenant filter to all entities implementing ITenantScoped
            if (typeof(ITenantScoped).IsAssignableFrom(clrType))
            {
                var method = typeof(MakenDbContext)
                    .GetMethod(nameof(ApplyTenantFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                    ?.MakeGenericMethod(clrType);

                method?.Invoke(null, new object[] { modelBuilder, _tenantContext });
            }
        }

        // Special case: User entity has nullable TenantId (for PlatformAdmin)
        // Apply custom filter that handles null tenant context AND soft delete
        modelBuilder.Entity<User>().HasQueryFilter(u =>
            !u.IsDeleted && (_tenantContext.TenantId == null || u.TenantId == _tenantContext.TenantId));
    }

    /// <summary>
    /// Applies soft delete filter to an entity type.
    /// Filter: WHERE IsDeleted = false
    /// </summary>
    private static void ApplySoftDeleteFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : BaseEntity
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(e => !e.IsDeleted);
    }

    /// <summary>
    /// Applies tenant scoping filter to an entity type.
    /// Filter: WHERE TenantId = @currentTenantId
    /// </summary>
    private static void ApplyTenantFilter<TEntity>(ModelBuilder modelBuilder, ITenantContext tenantContext)
        where TEntity : class, ITenantScoped
    {
        // IMPORTANT: The filter expression must capture the tenantContext variable
        // so it evaluates at query time, not at model creation time
        modelBuilder.Entity<TEntity>().HasQueryFilter(e => e.TenantId == tenantContext.TenantId);
    }

    /// <summary>
    /// Intercepts SaveChanges to enforce soft delete.
    /// Constitution requirement: No physical DELETE operations allowed.
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Intercept delete operations and convert to soft delete
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Deleted)
            {
                // Convert physical delete to soft delete
                entry.State = EntityState.Modified;
                entry.Entity.SoftDelete();
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
