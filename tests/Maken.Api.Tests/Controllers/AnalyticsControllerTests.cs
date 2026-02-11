using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using FluentAssertions;
using Maken.Api.DTOs.Responses;
using Maken.Application.Queries.Analytics;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Maken.Api.Tests.Controllers;

/// <summary>
/// Unit tests for AnalyticsController.
/// Tests analytics endpoints with proper authorization and tenant isolation.
/// </summary>
public class AnalyticsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<IMediator> _mediatorMock;

    public AnalyticsControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _mediatorMock = new Mock<IMediator>();
    }

    [Fact]
    public async Task GetAnalytics_WithValidRequest_ReturnsOkWithAnalyticsData()
    {
        // Arrange
        var analyticsResult = new AnalyticsResult(
            TotalEnrollments: 100,
            CompletionRate: 75.50m,
            ExamPassRate: 85.25m
        );

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetAnalyticsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(analyticsResult);

        var client = CreateAuthenticatedClient(role: "CompanyAdmin");

        // Act
        var response = await client.GetAsync("/api/analytics");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AnalyticsResponse>();
        result.Should().NotBeNull();
        result!.TotalEnrollments.Should().Be(100);
        result.CompletionRate.Should().Be(75.50m);
        result.ExamPassRate.Should().Be(85.25m);
    }

    [Fact]
    public async Task GetAnalytics_WithNoData_ReturnsZeroMetrics()
    {
        // Arrange
        var analyticsResult = new AnalyticsResult(
            TotalEnrollments: 0,
            CompletionRate: 0,
            ExamPassRate: 0
        );

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetAnalyticsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(analyticsResult);

        var client = CreateAuthenticatedClient(role: "CompanyAdmin");

        // Act
        var response = await client.GetAsync("/api/analytics");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AnalyticsResponse>();
        result.Should().NotBeNull();
        result!.TotalEnrollments.Should().Be(0);
        result.CompletionRate.Should().Be(0);
        result.ExamPassRate.Should().Be(0);
    }

    [Fact]
    public async Task GetAnalytics_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/analytics");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAnalytics_WithStudentRole_ReturnsForbidden()
    {
        // Arrange
        // No need to mock mediator - authorization should fail before reaching the handler
        var client = CreateAuthenticatedClient(role: "Student");

        // Act
        var response = await client.GetAsync("/api/analytics");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAnalytics_WithInstructorRole_ReturnsForbidden()
    {
        // Arrange
        // No need to mock mediator - authorization should fail before reaching the handler
        var client = CreateAuthenticatedClient(role: "Instructor");

        // Act
        var response = await client.GetAsync("/api/analytics");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAnalytics_WithCompanyAdminRole_ReturnsOk()
    {
        // Arrange
        var analyticsResult = new AnalyticsResult(
            TotalEnrollments: 50,
            CompletionRate: 60.00m,
            ExamPassRate: 70.00m
        );

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetAnalyticsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(analyticsResult);

        var client = CreateAuthenticatedClient(role: "CompanyAdmin");

        // Act
        var response = await client.GetAsync("/api/analytics");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAnalytics_WithHighCompletionRate_ReturnsCorrectData()
    {
        // Arrange
        var analyticsResult = new AnalyticsResult(
            TotalEnrollments: 200,
            CompletionRate: 95.75m,
            ExamPassRate: 92.50m
        );

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetAnalyticsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(analyticsResult);

        var client = CreateAuthenticatedClient(role: "CompanyAdmin");

        // Act
        var response = await client.GetAsync("/api/analytics");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AnalyticsResponse>();
        result.Should().NotBeNull();
        result!.CompletionRate.Should().Be(95.75m);
        result.ExamPassRate.Should().Be(92.50m);
    }

    [Fact]
    public async Task GetAnalytics_WithLowPassRate_ReturnsCorrectData()
    {
        // Arrange
        var analyticsResult = new AnalyticsResult(
            TotalEnrollments: 150,
            CompletionRate: 80.00m,
            ExamPassRate: 45.25m
        );

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetAnalyticsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(analyticsResult);

        var client = CreateAuthenticatedClient(role: "CompanyAdmin");

        // Act
        var response = await client.GetAsync("/api/analytics");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AnalyticsResponse>();
        result.Should().NotBeNull();
        result!.ExamPassRate.Should().Be(45.25m);
    }

    private HttpClient CreateAuthenticatedClient(string role = "CompanyAdmin", Guid? tenantId = null)
    {
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace IMediator with mock
                var mediatorDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IMediator));
                if (mediatorDescriptor != null)
                {
                    services.Remove(mediatorDescriptor);
                }
                services.AddSingleton(_mediatorMock.Object);

                // Add test authentication
                services.AddAuthentication("Test")
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });
            });
        }).CreateClient();

        // Set role and tenantId in headers for TestAuthHandler to read
        client.DefaultRequestHeaders.Add("X-Test-Role", role);
        client.DefaultRequestHeaders.Add("X-Test-TenantId", (tenantId ?? Guid.NewGuid()).ToString());

        return client;
    }

    private class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public TestAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // Get role from request headers (set by test)
            var role = Context.Request.Headers["X-Test-Role"].FirstOrDefault() ?? "CompanyAdmin";
            var tenantId = Context.Request.Headers["X-Test-TenantId"].FirstOrDefault() ?? Guid.NewGuid().ToString();

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, "Test User"),
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Role, role),
                new Claim("TenantId", tenantId)
            };

            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "Test");

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
