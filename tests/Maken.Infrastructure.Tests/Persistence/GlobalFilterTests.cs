using Maken.Application.Common.Interfaces;
using Maken.Domain.Entities;
using Maken.Domain.Enums;
using Maken.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Maken.Infrastructure.Tests.Persistence;

/// <summary>
/// Integration tests for global query filters.
/// Verifies Constitution requirements:
/// - Soft-deleted records are automatically excluded from queries
/// - IgnoreQueryFilters() can bypass the filter when needed
/// </summary>
public class GlobalFilterTests : IDisposable
{
    private readonly MakenDbContext _context;
    private readonly TestTenantContext _tenantContext;
    private readonly Guid _testTenantId;

    public GlobalFilterTests()
    {
        // Setup in-memory database for testing
        var options = new DbContextOptionsBuilder<MakenDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        // Create a mock tenant context
        _tenantContext = new TestTenantContext();
        _testTenantId = Guid.NewGuid();
        _tenantContext.SetTenantId(_testTenantId);

        _context = new MakenDbContext(options, _tenantContext);
    }

    [Fact]
    public async Task Query_ShouldExcludeSoftDeletedTenants()
    {
        // Arrange
        var activeTenant = new Tenant("Active Academy", "active");
        var deletedTenant = new Tenant("Deleted Academy", "deleted");
        deletedTenant.SoftDelete();

        _context.Tenants.AddRange(activeTenant, deletedTenant);
        await _context.SaveChangesAsync();

        // Act
        var tenants = await _context.Tenants.ToListAsync();

        // Assert
        Assert.Single(tenants);
        Assert.Equal("Active Academy", tenants[0].Name);
        Assert.DoesNotContain(tenants, t => t.Name == "Deleted Academy");
    }

    [Fact]
    public async Task Query_WithIgnoreQueryFilters_ShouldIncludeSoftDeletedTenants()
    {
        // Arrange
        var activeTenant = new Tenant("Active Academy", "active");
        var deletedTenant = new Tenant("Deleted Academy", "deleted");
        deletedTenant.SoftDelete();

        _context.Tenants.AddRange(activeTenant, deletedTenant);
        await _context.SaveChangesAsync();

        // Act
        var tenants = await _context.Tenants.IgnoreQueryFilters().ToListAsync();

        // Assert
        Assert.Equal(2, tenants.Count);
        Assert.Contains(tenants, t => t.Name == "Active Academy");
        Assert.Contains(tenants, t => t.Name == "Deleted Academy");
    }

    [Fact]
    public async Task Find_ShouldNotReturnSoftDeletedEntity()
    {
        // Arrange
        var tenant = new Tenant("Test Academy", "test");
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        var tenantId = tenant.Id;

        // Soft delete the tenant
        tenant.SoftDelete();
        await _context.SaveChangesAsync();

        // Clear the context to force a fresh query
        _context.ChangeTracker.Clear();

        // Act
        var foundTenant = await _context.Tenants.FindAsync(tenantId);

        // Assert
        Assert.Null(foundTenant);
    }

    [Fact]
    public async Task SaveChanges_WithDeletedEntity_ShouldConvertToSoftDelete()
    {
        // Arrange
        var tenant = new Tenant("Test Academy", "test");
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        var tenantId = tenant.Id;

        // Act - Try to physically delete
        _context.Tenants.Remove(tenant);
        await _context.SaveChangesAsync();

        // Assert - Should be soft deleted instead
        _context.ChangeTracker.Clear();
        var deletedTenant = await _context.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == tenantId);

        Assert.NotNull(deletedTenant);
        Assert.True(deletedTenant.IsDeleted);
        Assert.NotNull(deletedTenant.DeletedAt);
    }

    [Fact]
    public async Task Query_ShouldExcludeSoftDeletedUsers()
    {
        // Arrange
        var tenant = new Tenant("Test Academy", "test");
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        // Update tenant context to match the created tenant
        _tenantContext.SetTenantId(tenant.Id);

        var activeUser = new User(
            "active@test.com",
            "hashedpassword",
            "Active",
            "User",
            RoleType.Student,
            tenant.Id
        );

        var deletedUser = new User(
            "deleted@test.com",
            "hashedpassword",
            "Deleted",
            "User",
            RoleType.Student,
            tenant.Id
        );
        deletedUser.SoftDelete();

        _context.Users.AddRange(activeUser, deletedUser);
        await _context.SaveChangesAsync();

        // Clear context to force fresh query
        _context.ChangeTracker.Clear();

        // Act
        var users = await _context.Users.ToListAsync();

        // Assert
        Assert.Single(users);
        Assert.Equal("active@test.com", users[0].Email);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    /// <summary>
    /// Test implementation of ITenantContext for testing purposes.
    /// </summary>
    private class TestTenantContext : ITenantContext
    {
        private Guid? _tenantId;

        public Guid? TenantId => _tenantId;
        public string? Subdomain => "test";
        public bool HasTenant => _tenantId.HasValue;

        public void SetTenantId(Guid tenantId)
        {
            _tenantId = tenantId;
        }
    }
}
