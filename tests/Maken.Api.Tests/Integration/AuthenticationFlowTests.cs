using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Maken.Api.DTOs.Requests;
using Maken.Api.DTOs.Responses;
using Maken.Api.Tests.Helpers;
using Maken.Domain.Entities;
using Maken.Domain.Enums;
using Maken.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Maken.Api.Tests.Integration;

/// <summary>
/// Integration tests for authentication flow: login → refresh → tenant resolution.
/// Tests the complete end-to-end authentication workflow.
/// </summary>
public sealed class AuthenticationFlowTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private MakenDbContext _dbContext = null!;
    private Guid _tenantId;
    private Guid _userId;
    private const string TestEmail = "test@example.com";
    private const string TestPassword = "Test123!";
    private const string TestSubdomain = "testcompany";

    public AuthenticationFlowTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            // Configure test JWT settings BEFORE services are built
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:SecretKey"] = "TestSecretKeyForIntegrationTestsThatIsLongEnough123456",
                    ["Jwt:Issuer"] = "MakenTest",
                    ["Jwt:Audience"] = "MakenTest",
                    ["Jwt:AccessTokenExpirationMinutes"] = "60"
                });
            });
            
            builder.ConfigureServices(services =>
            {
                // Remove existing DbContext
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<MakenDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add in-memory database for testing
                services.AddDbContext<MakenDbContext>(options =>
                {
                    options.UseInMemoryDatabase("AuthFlowTestDb");
                });
            });
        });

        _client = _factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        var scope = _factory.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<MakenDbContext>();

        // Seed test data
        _tenantId = Guid.NewGuid();
        _userId = Guid.NewGuid();

        var tenant = new Tenant("Test Company", TestSubdomain);
        EntityTestHelper.SetId(tenant, _tenantId);

        // Hash the password using BCrypt
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(TestPassword);
        var user = new User(TestEmail, passwordHash, "Test", "User", RoleType.CompanyAdmin, _tenantId);
        EntityTestHelper.SetId(user, _userId);

        _dbContext.Tenants.Add(tenant);
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await _dbContext.Database.EnsureDeletedAsync();
        await _dbContext.DisposeAsync();
        _client.Dispose();
    }

    [Fact]
    public async Task CompleteAuthFlow_LoginRefreshTenantResolve_ShouldSucceed()
    {
        // Arrange
        var loginRequest = new LoginRequest(TestEmail, TestPassword);

        // Act 1: Login
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert 1: Login successful
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        loginResult.Should().NotBeNull();
        loginResult!.AccessToken.Should().NotBeNullOrEmpty();
        loginResult.RefreshToken.Should().NotBeNullOrEmpty();
        loginResult.UserId.Should().Be(_userId);
        loginResult.Email.Should().Be(TestEmail);

        // Act 2: Refresh token
        var refreshRequest = new RefreshTokenRequest(loginResult.RefreshToken);
        var refreshResponse = await _client.PostAsJsonAsync("/api/auth/refresh", refreshRequest);

        // Assert 2: Refresh successful
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var refreshResult = await refreshResponse.Content.ReadFromJsonAsync<LoginResponse>();
        refreshResult.Should().NotBeNull();
        refreshResult!.AccessToken.Should().NotBeNullOrEmpty();
        refreshResult.AccessToken.Should().NotBe(loginResult.AccessToken); // New token
        refreshResult.RefreshToken.Should().NotBeNullOrEmpty();

        // Act 3: Resolve tenant
        var tenantResponse = await _client.GetAsync($"/api/tenants/resolve?subdomain={TestSubdomain}");

        // Assert 3: Tenant resolution successful
        tenantResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tenantResult = await tenantResponse.Content.ReadFromJsonAsync<TenantResponse>();
        tenantResult.Should().NotBeNull();
        tenantResult!.Id.Should().Be(_tenantId);
        tenantResult.Subdomain.Should().Be(TestSubdomain);
        tenantResult.Name.Should().Be("Test Company");
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ShouldReturnUnauthorized()
    {
        // Arrange
        var loginRequest = new LoginRequest(TestEmail, "WrongPassword");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithNonExistentUser_ShouldReturnUnauthorized()
    {
        // Arrange
        var loginRequest = new LoginRequest("nonexistent@example.com", TestPassword);

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RefreshToken_WithInvalidToken_ShouldReturnUnauthorized()
    {
        // Arrange
        var refreshRequest = new RefreshTokenRequest("invalid-token");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/refresh", refreshRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RefreshToken_WithExpiredToken_ShouldReturnUnauthorized()
    {
        // Arrange - Create a refresh token that is already expired
        // Use EF Core's ability to set private properties
        var expiredToken = (RefreshToken)Activator.CreateInstance(typeof(RefreshToken), true)!;
        
        // Add to context first so EF can track it
        _dbContext.RefreshTokens.Add(expiredToken);
        
        // Use EF Core's entry to set private properties
        var entry = _dbContext.Entry(expiredToken);
        entry.Property("UserId").CurrentValue = _userId;
        entry.Property("Token").CurrentValue = "expired-token";
        entry.Property("ExpiresAt").CurrentValue = DateTime.UtcNow.AddDays(-1);
        entry.Property("IsRevoked").CurrentValue = false;
        entry.Property("CreatedAt").CurrentValue = DateTime.UtcNow.AddDays(-8);
        entry.Property("IsDeleted").CurrentValue = false;
        
        await _dbContext.SaveChangesAsync();

        var refreshRequest = new RefreshTokenRequest("expired-token");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/refresh", refreshRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TenantResolve_WithInvalidSubdomain_ShouldReturnNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/tenants/resolve?subdomain=nonexistent");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TenantResolve_WithEmptySubdomain_ShouldReturnBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/tenants/resolve?subdomain=");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_MultipleSuccessfulLogins_ShouldGenerateUniqueTokens()
    {
        // Arrange
        var loginRequest = new LoginRequest(TestEmail, TestPassword);

        // Act - Login twice
        var response1 = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var result1 = await response1.Content.ReadFromJsonAsync<LoginResponse>();

        var response2 = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var result2 = await response2.Content.ReadFromJsonAsync<LoginResponse>();

        // Assert - Tokens should be different
        result1.Should().NotBeNull();
        result2.Should().NotBeNull();
        result1!.AccessToken.Should().NotBe(result2!.AccessToken);
        result1.RefreshToken.Should().NotBe(result2.RefreshToken);
    }

    [Fact]
    public async Task RefreshToken_AfterSuccessfulRefresh_OldTokenShouldBeInvalidated()
    {
        // Arrange - Login first
        var loginRequest = new LoginRequest(TestEmail, TestPassword);
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        // Act 1 - Refresh with original token
        var refreshRequest1 = new RefreshTokenRequest(loginResult!.RefreshToken);
        var refreshResponse1 = await _client.PostAsJsonAsync("/api/auth/refresh", refreshRequest1);
        refreshResponse1.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act 2 - Try to refresh again with the same original token
        var refreshRequest2 = new RefreshTokenRequest(loginResult.RefreshToken);
        var refreshResponse2 = await _client.PostAsJsonAsync("/api/auth/refresh", refreshRequest2);

        // Assert - Second refresh with old token should fail
        refreshResponse2.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
