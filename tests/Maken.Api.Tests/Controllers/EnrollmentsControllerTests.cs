using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using FluentAssertions;
using Maken.Api.DTOs.Requests;
using Maken.Api.DTOs.Responses;
using Maken.Application.Commands.Students;
using Maken.Application.DTOs;
using Maken.Application.Queries.Enrollments;
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
/// Unit tests for EnrollmentsController.
/// Tests enrollment endpoints with proper authorization and tenant isolation.
/// </summary>
public class EnrollmentsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<IMediator> _mediatorMock;

    public EnrollmentsControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _mediatorMock = new Mock<IMediator>();
    }

    [Fact]
    public async Task GetEnrollments_WithValidCourseId_ReturnsOkWithEnrollmentList()
    {
        // Arrange
        var courseId = Guid.NewGuid();
        var studentId1 = Guid.NewGuid();
        var studentId2 = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var enrollments = new List<EnrollmentDto>
        {
            new EnrollmentDto(
                Id: Guid.NewGuid(),
                StudentId: studentId1,
                CourseId: courseId,
                TenantId: tenantId,
                EnrolledAt: DateTime.UtcNow.AddDays(-2),
                CompletedAt: null,
                StudentFirstName: "John",
                StudentLastName: "Doe",
                StudentEmail: "john.doe@example.com"
            ),
            new EnrollmentDto(
                Id: Guid.NewGuid(),
                StudentId: studentId2,
                CourseId: courseId,
                TenantId: tenantId,
                EnrolledAt: DateTime.UtcNow.AddDays(-1),
                CompletedAt: null,
                StudentFirstName: "Jane",
                StudentLastName: "Smith",
                StudentEmail: "jane.smith@example.com"
            )
        };

        _mediatorMock
            .Setup(m => m.Send(It.Is<GetEnrollmentsQuery>(q => q.CourseId == courseId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(enrollments);

        var client = CreateAuthenticatedClient(role: "CompanyAdmin");

        // Act
        var response = await client.GetAsync($"/api/courses/{courseId}/enrollments");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<EnrollmentResponse>>();
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result![0].StudentFirstName.Should().Be("John");
        result[1].StudentFirstName.Should().Be("Jane");
    }

    [Fact]
    public async Task GetEnrollments_WithNonExistentCourse_ReturnsNotFound()
    {
        // Arrange
        var courseId = Guid.NewGuid();

        _mediatorMock
            .Setup(m => m.Send(It.Is<GetEnrollmentsQuery>(q => q.CourseId == courseId), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException($"Course with ID '{courseId}' not found"));

        var client = CreateAuthenticatedClient(role: "CompanyAdmin");

        // Act
        var response = await client.GetAsync($"/api/courses/{courseId}/enrollments");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetEnrollments_WithoutAdminRole_ReturnsForbidden()
    {
        // Arrange
        var courseId = Guid.NewGuid();
        var client = CreateAuthenticatedClient(role: "Student");

        // Act
        var response = await client.GetAsync($"/api/courses/{courseId}/enrollments");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetEnrollments_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var courseId = Guid.NewGuid();
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/courses/{courseId}/enrollments");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task EnrollStudent_WithValidRequest_ReturnsCreatedWithEnrollment()
    {
        // Arrange
        var courseId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var enrollmentId = Guid.NewGuid();

        var request = new EnrollStudentRequest(StudentId: studentId);

        var enrollResult = new EnrollStudentResult(
            Id: enrollmentId,
            StudentId: studentId,
            CourseId: courseId,
            TenantId: tenantId,
            EnrolledAt: DateTime.UtcNow
        );

        var enrollmentDto = new EnrollmentDto(
            Id: enrollmentId,
            StudentId: studentId,
            CourseId: courseId,
            TenantId: tenantId,
            EnrolledAt: DateTime.UtcNow,
            CompletedAt: null,
            StudentFirstName: "John",
            StudentLastName: "Doe",
            StudentEmail: "john.doe@example.com"
        );

        _mediatorMock
            .Setup(m => m.Send(It.Is<EnrollStudentCommand>(c => c.StudentId == studentId && c.CourseId == courseId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(enrollResult);

        _mediatorMock
            .Setup(m => m.Send(It.Is<GetEnrollmentsQuery>(q => q.CourseId == courseId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EnrollmentDto> { enrollmentDto });

        var client = CreateAuthenticatedClient(role: "Instructor");

        // Act
        var response = await client.PostAsJsonAsync($"/api/courses/{courseId}/enrollments", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var result = await response.Content.ReadFromJsonAsync<EnrollmentResponse>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(enrollmentId);
        result.StudentId.Should().Be(studentId);
        result.CourseId.Should().Be(courseId);
        result.StudentFirstName.Should().Be("John");
        result.StudentLastName.Should().Be("Doe");
        result.StudentEmail.Should().Be("john.doe@example.com");
    }

    [Fact]
    public async Task EnrollStudent_WithNonExistentCourse_ReturnsNotFound()
    {
        // Arrange
        var courseId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var request = new EnrollStudentRequest(StudentId: studentId);

        _mediatorMock
            .Setup(m => m.Send(It.Is<EnrollStudentCommand>(c => c.StudentId == studentId && c.CourseId == courseId), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException($"Course with ID '{courseId}' not found"));

        var client = CreateAuthenticatedClient(role: "CompanyAdmin");

        // Act
        var response = await client.PostAsJsonAsync($"/api/courses/{courseId}/enrollments", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task EnrollStudent_WithNonExistentStudent_ReturnsNotFound()
    {
        // Arrange
        var courseId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var request = new EnrollStudentRequest(StudentId: studentId);

        _mediatorMock
            .Setup(m => m.Send(It.Is<EnrollStudentCommand>(c => c.StudentId == studentId && c.CourseId == courseId), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException($"Student with ID '{studentId}' not found"));

        var client = CreateAuthenticatedClient(role: "CompanyAdmin");

        // Act
        var response = await client.PostAsJsonAsync($"/api/courses/{courseId}/enrollments", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task EnrollStudent_WithExistingEnrollment_ReturnsBadRequest()
    {
        // Arrange
        var courseId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var request = new EnrollStudentRequest(StudentId: studentId);

        _mediatorMock
            .Setup(m => m.Send(It.Is<EnrollStudentCommand>(c => c.StudentId == studentId && c.CourseId == courseId), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Student is already enrolled in this course"));

        var client = CreateAuthenticatedClient(role: "CompanyAdmin");

        // Act
        var response = await client.PostAsJsonAsync($"/api/courses/{courseId}/enrollments", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task EnrollStudent_WithoutAdminRole_ReturnsForbidden()
    {
        // Arrange
        var courseId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var request = new EnrollStudentRequest(StudentId: studentId);

        var client = CreateAuthenticatedClient(role: "Student");

        // Act
        var response = await client.PostAsJsonAsync($"/api/courses/{courseId}/enrollments", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task EnrollStudent_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var courseId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var request = new EnrollStudentRequest(StudentId: studentId);

        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync($"/api/courses/{courseId}/enrollments", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetEnrollments_WithEmptyCourse_ReturnsEmptyList()
    {
        // Arrange
        var courseId = Guid.NewGuid();

        _mediatorMock
            .Setup(m => m.Send(It.Is<GetEnrollmentsQuery>(q => q.CourseId == courseId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EnrollmentDto>());

        var client = CreateAuthenticatedClient(role: "CompanyAdmin");

        // Act
        var response = await client.GetAsync($"/api/courses/{courseId}/enrollments");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<EnrollmentResponse>>();
        result.Should().NotBeNull();
        result!.Should().BeEmpty();
    }

    [Fact]
    public async Task GetEnrollments_WithCompletedEnrollments_ReturnsCompletedAt()
    {
        // Arrange
        var courseId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var completedAt = DateTime.UtcNow.AddDays(-1);

        var enrollments = new List<EnrollmentDto>
        {
            new EnrollmentDto(
                Id: Guid.NewGuid(),
                StudentId: studentId,
                CourseId: courseId,
                TenantId: tenantId,
                EnrolledAt: DateTime.UtcNow.AddDays(-10),
                CompletedAt: completedAt,
                StudentFirstName: "John",
                StudentLastName: "Doe",
                StudentEmail: "john.doe@example.com"
            )
        };

        _mediatorMock
            .Setup(m => m.Send(It.Is<GetEnrollmentsQuery>(q => q.CourseId == courseId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(enrollments);

        var client = CreateAuthenticatedClient(role: "CompanyAdmin");

        // Act
        var response = await client.GetAsync($"/api/courses/{courseId}/enrollments");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<EnrollmentResponse>>();
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result![0].CompletedAt.Should().NotBeNull();
        result[0].CompletedAt.Should().BeCloseTo(completedAt, TimeSpan.FromSeconds(1));
    }

    private HttpClient CreateAuthenticatedClient(string role = "CompanyAdmin")
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace IMediator with mock
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IMediator));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }
                services.AddSingleton(_mediatorMock.Object);

                // Add fake authentication with role
                services.AddAuthentication("Test")
                    .AddScheme<TestAuthOptions, TestAuthHandler>("Test", options =>
                    {
                        options.Role = role;
                    });
                services.AddAuthorization();
            });
        }).CreateClient();
    }

    /// <summary>
    /// Options for test authentication handler.
    /// </summary>
    private class TestAuthOptions : AuthenticationSchemeOptions
    {
        public string Role { get; set; } = "CompanyAdmin";
    }

    /// <summary>
    /// Test authentication handler that bypasses JWT validation.
    /// </summary>
    private class TestAuthHandler : AuthenticationHandler<TestAuthOptions>
    {
        public TestAuthHandler(
            IOptionsMonitor<TestAuthOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Email, "test@example.com"),
                new Claim(ClaimTypes.Role, Options.Role),
                new Claim("TenantId", Guid.NewGuid().ToString())
            };

            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "Test");

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
