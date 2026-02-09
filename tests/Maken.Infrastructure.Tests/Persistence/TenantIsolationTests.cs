using FluentAssertions;
using Maken.Application.Common.Interfaces;
using Maken.Domain.Entities;
using Maken.Domain.Enums;
using Maken.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Maken.Infrastructure.Tests.Persistence;

/// <summary>
/// Integration tests for cross-tenant data isolation.
/// Constitution requirement: Cross-tenant data access must be blocked at all times.
/// User Story 2: Tenant Data Isolation (Priority P1)
/// </summary>
public class TenantIsolationTests : IDisposable
{
    private readonly DbContextOptions<MakenDbContext> _options;
    private Guid _tenantAId;
    private Guid _tenantBId;

    public TenantIsolationTests()
    {
        // Use in-memory database for testing
        _options = new DbContextOptionsBuilder<MakenDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        // Seed test data
        SeedTestData();
    }

    private void SeedTestData()
    {
        // Create context without tenant scoping for seeding
        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.TenantId).Returns((Guid?)null);

        using var context = new MakenDbContext(_options, mockTenantContext.Object);

        // Create two tenants
        var tenantA = new Tenant("Academy A", "academy-a");
        var tenantB = new Tenant("Academy B", "academy-b");

        context.Tenants.AddRange(tenantA, tenantB);
        context.SaveChanges();

        // Store tenant IDs after they're saved
        _tenantAId = tenantA.Id;
        _tenantBId = tenantB.Id;

        // Create users for each tenant
        var userA1 = new User("user-a1@academy-a.com", "hashedpassword", "User", "A1", RoleType.Student, _tenantAId);
        var userA2 = new User("user-a2@academy-a.com", "hashedpassword", "User", "A2", RoleType.Instructor, _tenantAId);
        var userB1 = new User("user-b1@academy-b.com", "hashedpassword", "User", "B1", RoleType.Student, _tenantBId);
        var userB2 = new User("user-b2@academy-b.com", "hashedpassword", "User", "B2", RoleType.Instructor, _tenantBId);

        context.Users.AddRange(userA1, userA2, userB1, userB2);
        context.SaveChanges();
    }

    /// <summary>
    /// T043: Integration test - Cross-tenant query is blocked
    /// </summary>
    [Fact]
    public async Task QueryUsers_WithTenantAContext_ShouldOnlyReturnTenantAUsers()
    {
        // Arrange
        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.TenantId).Returns(_tenantAId);

        using var context = new MakenDbContext(_options, mockTenantContext.Object);

        // Act
        var users = await context.Users.ToListAsync();

        // Assert
        users.Should().HaveCount(2);
        users.Should().OnlyContain(u => u.TenantId == _tenantAId);
        users.Should().Contain(u => u.Email == "user-a1@academy-a.com");
        users.Should().Contain(u => u.Email == "user-a2@academy-a.com");
    }

    /// <summary>
    /// T043: Integration test - Cross-tenant query is blocked (Tenant B)
    /// </summary>
    [Fact]
    public async Task QueryUsers_WithTenantBContext_ShouldOnlyReturnTenantBUsers()
    {
        // Arrange
        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.TenantId).Returns(_tenantBId);

        using var context = new MakenDbContext(_options, mockTenantContext.Object);

        // Act
        var users = await context.Users.ToListAsync();

        // Assert
        users.Should().HaveCount(2);
        users.Should().OnlyContain(u => u.TenantId == _tenantBId);
        users.Should().Contain(u => u.Email == "user-b1@academy-b.com");
        users.Should().Contain(u => u.Email == "user-b2@academy-b.com");
    }

    /// <summary>
    /// T043: Integration test - Cannot access specific entity from another tenant
    /// </summary>
    [Fact]
    public async Task FindUser_FromDifferentTenant_ShouldReturnNull()
    {
        // Arrange - Get a user ID from Tenant B
        Guid userB1Id;
        var mockTenantContextB = new Mock<ITenantContext>();
        mockTenantContextB.Setup(x => x.TenantId).Returns(_tenantBId);

        using (var contextB = new MakenDbContext(_options, mockTenantContextB.Object))
        {
            var userB1 = await contextB.Users.FirstAsync(u => u.Email == "user-b1@academy-b.com");
            userB1Id = userB1.Id;
        }

        // Act - Try to access that user from Tenant A context
        var mockTenantContextA = new Mock<ITenantContext>();
        mockTenantContextA.Setup(x => x.TenantId).Returns(_tenantAId);

        using var contextA = new MakenDbContext(_options, mockTenantContextA.Object);
        var result = await contextA.Users.FirstOrDefaultAsync(u => u.Id == userB1Id);

        // Assert
        result.Should().BeNull("User from Tenant B should not be accessible from Tenant A context");
    }

    /// <summary>
    /// T043: Integration test - PlatformAdmin (no tenant context) can see all tenants
    /// </summary>
    [Fact]
    public async Task QueryTenants_WithoutTenantContext_ShouldReturnAllTenants()
    {
        // Arrange - PlatformAdmin has no tenant context
        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.TenantId).Returns((Guid?)null);

        using var context = new MakenDbContext(_options, mockTenantContext.Object);

        // Act
        var tenants = await context.Tenants.ToListAsync();

        // Assert
        tenants.Should().HaveCount(2);
        tenants.Should().Contain(t => t.Subdomain == "academy-a");
        tenants.Should().Contain(t => t.Subdomain == "academy-b");
    }

    /// <summary>
    /// T043: Integration test - Cannot update entity from another tenant
    /// </summary>
    [Fact]
    public async Task UpdateUser_FromDifferentTenant_ShouldNotAffectEntity()
    {
        // Arrange - Get a user from Tenant B
        Guid userB1Id;
        var mockTenantContextB = new Mock<ITenantContext>();
        mockTenantContextB.Setup(x => x.TenantId).Returns(_tenantBId);

        using (var contextB = new MakenDbContext(_options, mockTenantContextB.Object))
        {
            var userB1 = await contextB.Users.FirstAsync(u => u.Email == "user-b1@academy-b.com");
            userB1Id = userB1.Id;
        }

        // Act - Try to update that user from Tenant A context
        var mockTenantContextA = new Mock<ITenantContext>();
        mockTenantContextA.Setup(x => x.TenantId).Returns(_tenantAId);

        using (var contextA = new MakenDbContext(_options, mockTenantContextA.Object))
        {
            var user = await contextA.Users.FirstOrDefaultAsync(u => u.Id == userB1Id);
            user.Should().BeNull("Cannot find user from different tenant");
        }

        // Assert - Verify user in Tenant B is unchanged
        using (var contextB = new MakenDbContext(_options, mockTenantContextB.Object))
        {
            var userB1 = await contextB.Users.FirstAsync(u => u.Id == userB1Id);
            userB1.Email.Should().Be("user-b1@academy-b.com");
        }
    }

    /// <summary>
    /// T043: Integration test - Cannot delete entity from another tenant
    /// </summary>
    [Fact]
    public async Task DeleteUser_FromDifferentTenant_ShouldNotAffectEntity()
    {
        // Arrange - Get a user from Tenant B
        Guid userB1Id;
        var mockTenantContextB = new Mock<ITenantContext>();
        mockTenantContextB.Setup(x => x.TenantId).Returns(_tenantBId);

        using (var contextB = new MakenDbContext(_options, mockTenantContextB.Object))
        {
            var userB1 = await contextB.Users.FirstAsync(u => u.Email == "user-b1@academy-b.com");
            userB1Id = userB1.Id;
        }

        // Act - Try to delete that user from Tenant A context
        var mockTenantContextA = new Mock<ITenantContext>();
        mockTenantContextA.Setup(x => x.TenantId).Returns(_tenantAId);

        using (var contextA = new MakenDbContext(_options, mockTenantContextA.Object))
        {
            var user = await contextA.Users.FirstOrDefaultAsync(u => u.Id == userB1Id);
            user.Should().BeNull("Cannot find user from different tenant to delete");
        }

        // Assert - Verify user in Tenant B still exists
        using (var contextB = new MakenDbContext(_options, mockTenantContextB.Object))
        {
            var userB1 = await contextB.Users.FirstOrDefaultAsync(u => u.Id == userB1Id);
            userB1.Should().NotBeNull("User should still exist in Tenant B");
            userB1!.IsDeleted.Should().BeFalse();
        }
    }

    public void Dispose()
    {
        // Cleanup in-memory database
        using var context = new MakenDbContext(_options, Mock.Of<ITenantContext>());
        context.Database.EnsureDeleted();
    }
}
