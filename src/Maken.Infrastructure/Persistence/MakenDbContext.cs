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
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<Exam> Exams => Set<Exam>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<Choice> Choices => Set<Choice>();
    public DbSet<Progress> Progresses => Set<Progress>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<ContactInquiry> ContactInquiries => Set<ContactInquiry>();

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
        // Get the current tenant ID - this will be evaluated at query execution time
        // Note: We access _tenantContext.TenantId directly in the lambda, which EF Core
        // will evaluate each time a query is executed, not when the DbContext is created
        
        // Apply filters to specific entity types
        // Course: BaseEntity + ITenantScoped
        modelBuilder.Entity<Course>().HasQueryFilter(c => 
            !c.IsDeleted && (_tenantContext.TenantId == null || c.TenantId == _tenantContext.TenantId));

        // Lesson: BaseEntity with TenantId property (but doesn't implement ITenantScoped)
        // Apply both soft delete and tenant filter
        modelBuilder.Entity<Lesson>().HasQueryFilter(l => 
            !l.IsDeleted && (_tenantContext.TenantId == null || l.TenantId == _tenantContext.TenantId));

        // Exam: BaseEntity + ITenantScoped
        modelBuilder.Entity<Exam>().HasQueryFilter(e => 
            !e.IsDeleted && (_tenantContext.TenantId == null || e.TenantId == _tenantContext.TenantId));

        // Question: BaseEntity only (no tenant scoping - inherits from Exam)
        modelBuilder.Entity<Question>().HasQueryFilter(q => !q.IsDeleted);

        // Choice: BaseEntity only (no tenant scoping - inherits from Question)
        modelBuilder.Entity<Choice>().HasQueryFilter(c => !c.IsDeleted);

        // Progress: BaseEntity + ITenantScoped
        modelBuilder.Entity<Progress>().HasQueryFilter(p => 
            !p.IsDeleted && (_tenantContext.TenantId == null || p.TenantId == _tenantContext.TenantId));

        // Enrollment: BaseEntity + ITenantScoped
        modelBuilder.Entity<Enrollment>().HasQueryFilter(e => 
            !e.IsDeleted && (_tenantContext.TenantId == null || e.TenantId == _tenantContext.TenantId));

        // RefreshToken: BaseEntity only
        modelBuilder.Entity<RefreshToken>().HasQueryFilter(rt => !rt.IsDeleted);

        // Tenant: BaseEntity only (no tenant scoping for Tenant entity itself)
        modelBuilder.Entity<Tenant>().HasQueryFilter(t => !t.IsDeleted);

        // User: Special case - nullable TenantId for PlatformAdmin
        modelBuilder.Entity<User>().HasQueryFilter(u =>
            !u.IsDeleted && (_tenantContext.TenantId == null || u.TenantId == _tenantContext.TenantId));

        // ContactInquiry: BaseEntity only (no tenant scoping - public inquiries from landing page)
        modelBuilder.Entity<ContactInquiry>().HasQueryFilter(ci => !ci.IsDeleted);
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
