using System.Net;
using System.Net.Http.Headers;
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
/// Integration tests for tenant isolation:
/// Create course in tenant A → Try to access from tenant B → Verify 404
/// Validates that users cannot access resources from other tenants.
/// </summary>
public sealed class TenantIsolationIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private MakenDbContext _dbContext = null!;
    private Guid _tenantAId;
    private Guid _tenantBId;
    private Guid _instructorAId;
    private Guid _instructorBId;
    private string _instructorAToken = string.Empty;
    private string _instructorBToken = string.Empty;

    public TenantIsolationIntegrationTests(WebApplicationFactory<Program> factory)
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
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<MakenDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<MakenDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TenantIsolationTestDb");
                });
            });
        });

        _client = _factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        var scope = _factory.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<MakenDbContext>();

        // Seed test data for two tenants
        _tenantAId = Guid.NewGuid();
        _tenantBId = Guid.NewGuid();
        _instructorAId = Guid.NewGuid();
        _instructorBId = Guid.NewGuid();

        var tenantA = new Tenant("Company A", "companya");
        EntityTestHelper.SetId(tenantA, _tenantAId);

        var tenantB = new Tenant("Company B", "companyb");
        EntityTestHelper.SetId(tenantB, _tenantBId);

        var instructorA = new User("instructorA@companya.com", BCrypt.Net.BCrypt.HashPassword("Test123!"), 
            "John", "InstructorA", RoleType.Instructor, _tenantAId);
        EntityTestHelper.SetId(instructorA, _instructorAId);

        var instructorB = new User("instructorB@companyb.com", BCrypt.Net.BCrypt.HashPassword("Test123!"), 
            "Jane", "InstructorB", RoleType.Instructor, _tenantBId);
        EntityTestHelper.SetId(instructorB, _instructorBId);

        _dbContext.Tenants.Add(tenantA);
        _dbContext.Tenants.Add(tenantB);
        _dbContext.Users.Add(instructorA);
        _dbContext.Users.Add(instructorB);
        await _dbContext.SaveChangesAsync();

        // Get tokens for both instructors
        _instructorAToken = await GetAuthToken("instructorA@companya.com", "Test123!");
        _instructorBToken = await GetAuthToken("instructorB@companyb.com", "Test123!");
    }

    public async Task DisposeAsync()
    {
        await _dbContext.Database.EnsureDeletedAsync();
        await _dbContext.DisposeAsync();
        _client.Dispose();
    }

    private async Task<string> GetAuthToken(string email, string password)
    {
        var loginRequest = new LoginRequest(email, password);
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return result!.AccessToken;
    }

    [Fact]
    public async Task TenantIsolation_CourseCreatedInTenantA_ShouldNotBeAccessibleFromTenantB()
    {
        // Arrange: Instructor A creates a course
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _instructorAToken);
        
        var createCourseRequest = new CreateCourseRequest(
            "Tenant A Course",
            "This course belongs to Tenant A",
            false
        );
        
        var courseResponse = await _client.PostAsJsonAsync("/api/courses", createCourseRequest);
        courseResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var course = await courseResponse.Content.ReadFromJsonAsync<CourseResponse>();
        var courseId = course!.Id;

        // Act: Instructor B tries to access the course
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _instructorBToken);
        var accessResponse = await _client.GetAsync($"/api/courses/{courseId}");

        // Assert: Should return 404 (not found due to tenant isolation)
        accessResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TenantIsolation_LessonCreatedInTenantA_ShouldNotBeAccessibleFromTenantB()
    {
        // Arrange: Instructor A creates a course and lesson
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _instructorAToken);
        
        var createCourseRequest = new CreateCourseRequest("Course A", "Description", false);
        var courseResponse = await _client.PostAsJsonAsync("/api/courses", createCourseRequest);
        var course = await courseResponse.Content.ReadFromJsonAsync<CourseResponse>();
        var courseId = course!.Id;

        var createLessonRequest = new CreateLessonRequest(
            "Lesson A",
            "Description",
            "Video",
            "https://example.com/video.mp4",
            1
        );
        
        var lessonResponse = await _client.PostAsJsonAsync($"/api/courses/{courseId}/lessons", createLessonRequest);
        lessonResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var lesson = await lessonResponse.Content.ReadFromJsonAsync<LessonResponse>();
        var lessonId = lesson!.Id;

        // Act: Instructor B tries to access the lesson
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _instructorBToken);
        var accessResponse = await _client.GetAsync($"/api/courses/{courseId}/lessons/{lessonId}");

        // Assert: Should return 404
        accessResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TenantIsolation_ExamCreatedInTenantA_ShouldNotBeAccessibleFromTenantB()
    {
        // Arrange: Instructor A creates a course, lesson, and exam
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _instructorAToken);
        
        var createCourseRequest = new CreateCourseRequest("Course A", "Description", false);
        var courseResponse = await _client.PostAsJsonAsync("/api/courses", createCourseRequest);
        var course = await courseResponse.Content.ReadFromJsonAsync<CourseResponse>();
        var courseId = course!.Id;

        var createLessonRequest = new CreateLessonRequest("Lesson A", "Description", "Video", "https://example.com/video.mp4", 1);
        var lessonResponse = await _client.PostAsJsonAsync($"/api/courses/{courseId}/lessons", createLessonRequest);
        var lesson = await lessonResponse.Content.ReadFromJsonAsync<LessonResponse>();
        var lessonId = lesson!.Id;

        var createExamRequest = new CreateExamRequest(
            lessonId,
            "Exam A",
            70,
            new List<CreateQuestionRequest>
            {
                new CreateQuestionRequest(
                    "Question 1?",
                    1,
                    new List<CreateChoiceRequest>
                    {
                        new CreateChoiceRequest("Answer 1", true),
                        new CreateChoiceRequest("Answer 2", false)
                    }
                )
            }
        );
        
        var examResponse = await _client.PostAsJsonAsync("/api/exams", createExamRequest);
        examResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var exam = await examResponse.Content.ReadFromJsonAsync<ExamResponse>();
        var examId = exam!.Id;

        // Act: Instructor B tries to access the exam
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _instructorBToken);
        var accessResponse = await _client.GetAsync($"/api/exams/{examId}");

        // Assert: Should return 404
        accessResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TenantIsolation_InstructorBCannotUpdateCourseFromTenantA()
    {
        // Arrange: Instructor A creates a course
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _instructorAToken);
        
        var createCourseRequest = new CreateCourseRequest("Course A", "Description", false);
        var courseResponse = await _client.PostAsJsonAsync("/api/courses", createCourseRequest);
        var course = await courseResponse.Content.ReadFromJsonAsync<CourseResponse>();
        var courseId = course!.Id;

        // Act: Instructor B tries to update the course
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _instructorBToken);
        
        var updateCourseRequest = new UpdateCourseRequest(
            "Updated Course Name",
            "Updated Description",
            false
        );
        
        var updateResponse = await _client.PutAsJsonAsync($"/api/courses/{courseId}", updateCourseRequest);

        // Assert: Should return 404
        updateResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TenantIsolation_InstructorBCannotDeleteCourseFromTenantA()
    {
        // Arrange: Instructor A creates a course
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _instructorAToken);
        
        var createCourseRequest = new CreateCourseRequest("Course A", "Description", false);
        var courseResponse = await _client.PostAsJsonAsync("/api/courses", createCourseRequest);
        var course = await courseResponse.Content.ReadFromJsonAsync<CourseResponse>();
        var courseId = course!.Id;

        // Act: Instructor B tries to delete the course
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _instructorBToken);
        var deleteResponse = await _client.DeleteAsync($"/api/courses/{courseId}");

        // Assert: Should return 404
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TenantIsolation_GetCoursesQuery_ShouldOnlyReturnCoursesFromSameTenant()
    {
        // Arrange: Both instructors create courses
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _instructorAToken);
        await _client.PostAsJsonAsync("/api/courses", new CreateCourseRequest("Course A1", "Description", false));
        await _client.PostAsJsonAsync("/api/courses", new CreateCourseRequest("Course A2", "Description", false));

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _instructorBToken);
        await _client.PostAsJsonAsync("/api/courses", new CreateCourseRequest("Course B1", "Description", false));

        // Act: Instructor A queries all courses
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _instructorAToken);
        var coursesAResponse = await _client.GetAsync("/api/courses?pageNumber=1&pageSize=20");
        var coursesA = await coursesAResponse.Content.ReadFromJsonAsync<CourseListResponse>();

        // Act: Instructor B queries all courses
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _instructorBToken);
        var coursesBResponse = await _client.GetAsync("/api/courses?pageNumber=1&pageSize=20");
        var coursesB = await coursesBResponse.Content.ReadFromJsonAsync<CourseListResponse>();

        // Assert: Each instructor should only see their own tenant's courses
        coursesA.Should().NotBeNull();
        coursesA!.Courses.Should().HaveCount(2);
        coursesA.Courses.Should().AllSatisfy(c => c.Name.Should().StartWith("Course A"));

        coursesB.Should().NotBeNull();
        coursesB!.Courses.Should().HaveCount(1);
        coursesB.Courses[0].Name.Should().Be("Course B1");
    }

    [Fact]
    public async Task TenantIsolation_EnrollmentAcrossTenants_ShouldFail()
    {
        // Arrange: Create student in Tenant B
        var studentBId = Guid.NewGuid();
        var studentB = new User("studentB@companyb.com", BCrypt.Net.BCrypt.HashPassword("Test123!"), 
            "Bob", "StudentB", RoleType.Student, _tenantBId);
        EntityTestHelper.SetId(studentB, studentBId);
        _dbContext.Users.Add(studentB);
        await _dbContext.SaveChangesAsync();

        // Instructor A creates a course
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _instructorAToken);
        var createCourseRequest = new CreateCourseRequest("Course A", "Description", false);
        var courseResponse = await _client.PostAsJsonAsync("/api/courses", createCourseRequest);
        var course = await courseResponse.Content.ReadFromJsonAsync<CourseResponse>();
        var courseId = course!.Id;

        await _client.PostAsync($"/api/courses/{courseId}/publish", null);

        // Act: Instructor A tries to enroll student from Tenant B
        var enrollRequest = new EnrollStudentRequest(studentBId);
        var enrollResponse = await _client.PostAsJsonAsync($"/api/courses/{courseId}/enrollments", enrollRequest);

        // Assert: Should fail (403 Forbidden is appropriate for cross-tenant enrollment attempts)
        enrollResponse.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }
}
