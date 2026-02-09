using FluentAssertions;
using Maken.Api.Middleware;
using Maken.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace Maken.Api.Tests.Middleware;

/// <summary>
/// Tests for TenantMiddleware - subdomain extraction and tenant resolution.
/// Constitution requirement: Tenant must be resolved from subdomain (e.g., academy.maken.app).
/// User Story 2: Tenant Data Isolation (Priority P1)
/// </summary>
public class TenantMiddlewareTests
{
    private readonly Mock<ITenantResolver> _mockTenantResolver;
    private readonly Mock<RequestDelegate> _mockNext;

    public TenantMiddlewareTests()
    {
        _mockTenantResolver = new Mock<ITenantResolver>();
        _mockNext = new Mock<RequestDelegate>();
    }

    /// <summary>
    /// T040: Unit test - Subdomain extraction from Host header
    /// </summary>
    [Theory]
    [InlineData("academy.maken.app", "academy")]
    [InlineData("institute.maken.app", "institute")]
    [InlineData("test-school.maken.app", "test-school")]
    [InlineData("maken.app", null)] // Root domain, no subdomain
    [InlineData("localhost", null)] // Local development
    [InlineData("localhost:5000", null)] // Local with port
    public async Task InvokeAsync_ShouldExtractSubdomainFromHost(string host, string? expectedSubdomain)
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString(host);

        var tenantId = expectedSubdomain != null ? Guid.NewGuid() : (Guid?)null;
        _mockTenantResolver
            .Setup(x => x.ResolveAsync(expectedSubdomain, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenantId);

        var middleware = new TenantMiddleware(_mockNext.Object);

        // Act
        await middleware.InvokeAsync(context, _mockTenantResolver.Object);

        // Assert
        if (expectedSubdomain != null)
        {
            context.Items.Should().ContainKey("Subdomain");
            context.Items["Subdomain"].Should().Be(expectedSubdomain);
        }
        else
        {
            context.Items.Should().NotContainKey("Subdomain");
        }
    }

    /// <summary>
    /// T041: Integration test - Valid subdomain resolves tenant
    /// </summary>
    [Fact]
    public async Task InvokeAsync_WithValidSubdomain_ShouldResolveTenant()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("academy.maken.app");

        var expectedTenantId = Guid.NewGuid();
        _mockTenantResolver
            .Setup(x => x.ResolveAsync("academy", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTenantId);

        var middleware = new TenantMiddleware(_mockNext.Object);

        // Act
        await middleware.InvokeAsync(context, _mockTenantResolver.Object);

        // Assert
        context.Items.Should().ContainKey("TenantId");
        context.Items["TenantId"].Should().Be(expectedTenantId);
        context.Items.Should().ContainKey("Subdomain");
        context.Items["Subdomain"].Should().Be("academy");

        _mockNext.Verify(x => x(context), Times.Once);
    }

    /// <summary>
    /// T042: Integration test - Invalid subdomain returns 404
    /// </summary>
    [Fact]
    public async Task InvokeAsync_WithInvalidSubdomain_ShouldReturn404()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("unknown.maken.app");
        context.Response.Body = new MemoryStream();

        _mockTenantResolver
            .Setup(x => x.ResolveAsync("unknown", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid?)null);

        var middleware = new TenantMiddleware(_mockNext.Object);

        // Act
        await middleware.InvokeAsync(context, _mockTenantResolver.Object);

        // Assert
        context.Response.StatusCode.Should().Be(404);
        _mockNext.Verify(x => x(context), Times.Never);
    }

    /// <summary>
    /// T042: Integration test - Reserved subdomain returns 404
    /// </summary>
    [Theory]
    [InlineData("www.maken.app")]
    [InlineData("api.maken.app")]
    [InlineData("admin.maken.app")]
    [InlineData("app.maken.app")]
    public async Task InvokeAsync_WithReservedSubdomain_ShouldReturn404(string host)
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString(host);
        context.Response.Body = new MemoryStream();

        var subdomain = host.Split('.')[0];
        _mockTenantResolver
            .Setup(x => x.IsReservedSubdomain(subdomain))
            .Returns(true);

        var middleware = new TenantMiddleware(_mockNext.Object);

        // Act
        await middleware.InvokeAsync(context, _mockTenantResolver.Object);

        // Assert
        context.Response.StatusCode.Should().Be(404);
        _mockNext.Verify(x => x(context), Times.Never);
        _mockTenantResolver.Verify(x => x.ResolveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Test - Root domain (no subdomain) allows request to proceed without tenant
    /// This is for PlatformAdmin operations
    /// </summary>
    [Fact]
    public async Task InvokeAsync_WithRootDomain_ShouldProceedWithoutTenant()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("maken.app");

        var middleware = new TenantMiddleware(_mockNext.Object);

        // Act
        await middleware.InvokeAsync(context, _mockTenantResolver.Object);

        // Assert
        context.Items.Should().NotContainKey("TenantId");
        context.Items.Should().NotContainKey("Subdomain");
        _mockNext.Verify(x => x(context), Times.Once);
    }

    /// <summary>
    /// Test - Localhost allows request to proceed without tenant (development)
    /// </summary>
    [Theory]
    [InlineData("localhost")]
    [InlineData("localhost:5000")]
    [InlineData("127.0.0.1")]
    [InlineData("127.0.0.1:5000")]
    public async Task InvokeAsync_WithLocalhost_ShouldProceedWithoutTenant(string host)
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString(host);

        var middleware = new TenantMiddleware(_mockNext.Object);

        // Act
        await middleware.InvokeAsync(context, _mockTenantResolver.Object);

        // Assert
        context.Items.Should().NotContainKey("TenantId");
        context.Items.Should().NotContainKey("Subdomain");
        _mockNext.Verify(x => x(context), Times.Once);
    }
}
