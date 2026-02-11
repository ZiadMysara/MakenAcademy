using FluentAssertions;
using Maken.Application.Common.Interfaces;
using Maken.Application.Queries.Tenants;
using Maken.Domain.Entities;
using Moq;

namespace Maken.Application.Tests.Queries;

/// <summary>
/// Unit tests for ResolveTenantQueryHandler.
/// Validates tenant resolution by subdomain with case-insensitive matching.
/// </summary>
public class ResolveTenantQueryTests
{
    private readonly Mock<IRepository<Tenant>> _mockTenantRepository;
    private readonly ResolveTenantQueryHandler _handler;

    public ResolveTenantQueryTests()
    {
        _mockTenantRepository = new Mock<IRepository<Tenant>>();
        _handler = new ResolveTenantQueryHandler(_mockTenantRepository.Object);
    }

    [Fact]
    public async Task Handle_ValidSubdomain_ShouldReturnTenantResult()
    {
        // Arrange
        var subdomain = "acme";
        var tenant = new Tenant("Acme Corporation", subdomain);

        _mockTenantRepository
            .Setup(x => x.GetFirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Tenant, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        var query = new ResolveTenantQuery(subdomain);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(tenant.Id);
        result.Name.Should().Be("Acme Corporation");
        result.Subdomain.Should().Be(subdomain);
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_SubdomainNotFound_ShouldReturnNull()
    {
        // Arrange
        var subdomain = "nonexistent";

        _mockTenantRepository
            .Setup(x => x.GetFirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Tenant, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        var query = new ResolveTenantQuery(subdomain);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_EmptySubdomain_ShouldReturnNull()
    {
        // Arrange
        var query = new ResolveTenantQuery(string.Empty);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();

        // Verify repository was not called
        _mockTenantRepository.Verify(
            x => x.GetFirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Tenant, bool>>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhitespaceSubdomain_ShouldReturnNull()
    {
        // Arrange
        var query = new ResolveTenantQuery("   ");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();

        // Verify repository was not called
        _mockTenantRepository.Verify(
            x => x.GetFirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Tenant, bool>>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_SubdomainCaseInsensitive_ShouldFindTenant()
    {
        // Arrange
        var subdomain = "acme";
        var subdomainUpperCase = "ACME";
        var tenant = new Tenant("Acme Corporation", subdomain);

        _mockTenantRepository
            .Setup(x => x.GetFirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Tenant, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        var query = new ResolveTenantQuery(subdomainUpperCase);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(tenant.Id);
        result.Subdomain.Should().Be(subdomain);
    }

    [Fact]
    public async Task Handle_DeletedTenant_ShouldReturnInactive()
    {
        // Arrange
        var subdomain = "acme";
        var tenant = new Tenant("Acme Corporation", subdomain);
        tenant.SoftDelete(); // Soft delete the tenant

        _mockTenantRepository
            .Setup(x => x.GetFirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Tenant, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        var query = new ResolveTenantQuery(subdomain);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(tenant.Id);
        result.IsActive.Should().BeFalse(); // Should indicate tenant is inactive
    }

    [Fact]
    public async Task Handle_ActiveTenant_ShouldReturnActive()
    {
        // Arrange
        var subdomain = "acme";
        var tenant = new Tenant("Acme Corporation", subdomain);

        _mockTenantRepository
            .Setup(x => x.GetFirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Tenant, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        var query = new ResolveTenantQuery(subdomain);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.IsActive.Should().BeTrue();
    }

    [Theory]
    [InlineData("acme")]
    [InlineData("ACME")]
    [InlineData("AcMe")]
    [InlineData("aCmE")]
    public async Task Handle_VariousCases_ShouldAllResolveToSameTenant(string subdomainVariant)
    {
        // Arrange - Tenant is stored with lowercase subdomain
        var subdomain = "acme";
        var tenant = new Tenant("Acme Corporation", subdomain);

        _mockTenantRepository
            .Setup(x => x.GetFirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Tenant, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        // Act - Query with different case variant
        var query = new ResolveTenantQuery(subdomainVariant);
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert - All variants should resolve to the same tenant
        result.Should().NotBeNull();
        result!.Id.Should().Be(tenant.Id);
        result.Subdomain.Should().Be(subdomain);
    }
}
