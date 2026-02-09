using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Maken.Api.Models;
using Maken.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Maken.Api.Tests.Controllers;

public class HealthControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HealthControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetHealth_WhenDatabaseConnected_ReturnsHealthy()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var healthResponse = await response.Content.ReadFromJsonAsync<HealthResponse>();
        healthResponse.Should().NotBeNull();
        healthResponse!.Status.Should().Be("healthy");
        healthResponse.Version.Should().Be("1.0.0");
        healthResponse.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        healthResponse.Checks.Should().ContainSingle(c => c.Name == "database");

        var dbCheck = healthResponse.Checks.First(c => c.Name == "database");
        dbCheck.Status.Should().Be("healthy");
        dbCheck.Duration.Should().NotBeNullOrEmpty();
        dbCheck.Duration.Should().EndWith("ms");
    }

    [Fact]
    public async Task GetHealth_DoesNotRequireAuthentication()
    {
        // Arrange
        var client = _factory.CreateClient();
        // No authentication headers added

        // Act
        var response = await client.GetAsync("/api/v1/health");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetHealth_ReturnsValidTimestamp()
    {
        // Arrange
        var client = _factory.CreateClient();
        var beforeRequest = DateTime.UtcNow;

        // Act
        var response = await client.GetAsync("/api/v1/health");
        var afterRequest = DateTime.UtcNow;

        // Assert
        var healthResponse = await response.Content.ReadFromJsonAsync<HealthResponse>();
        healthResponse.Should().NotBeNull();
        healthResponse!.Timestamp.Should().BeOnOrAfter(beforeRequest);
        healthResponse.Timestamp.Should().BeOnOrBefore(afterRequest);
    }

    [Fact]
    public async Task GetHealth_DatabaseCheck_IncludesDuration()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/health");

        // Assert
        var healthResponse = await response.Content.ReadFromJsonAsync<HealthResponse>();
        var dbCheck = healthResponse!.Checks.First(c => c.Name == "database");

        dbCheck.Duration.Should().NotBeNullOrEmpty();
        dbCheck.Duration.Should().MatchRegex(@"^\d+ms$");
    }
}
