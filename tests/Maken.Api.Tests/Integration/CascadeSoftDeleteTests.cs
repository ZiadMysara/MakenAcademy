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
/// Integration tests for cascade soft delete:
/// Create course with lessons and exams → Soft delete course → Verify all related entities are soft deleted
/// </summary>
public sealed class CascadeSoftDeleteTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private MakenDbContext _dbContext = null!;
    private Guid _tenantId;
    private Guid _instructorId;
    private string _instructorToken = string.Empty;

    public CascadeSoftDeleteTests(WebApplicationFactory<Program> factory)
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
                    options.UseInMemoryDatabase("CascadeSoftDeleteTestDb");
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
        _instructorId = Guid.NewGuid();

        var tenant = new Tenant("Test Company", "testcompany");
        EntityTestHelper.SetId(tenant, _tenantId);

        var instructor = new User("instructor@test.com", BCrypt.Net.BCrypt.HashPassword("Test123!"), 
            "John", "Instructor", RoleType.Instructor, _tenantId);
        EntityTestHelper.SetId(instructor, _instructorId);

        _dbContext.Tenants.Add(tenant);
        _dbContext.Users.Add(instructor);
        await _dbContext.SaveChangesAsync();

        _instructorToken = await GetAuthToken("instructor@test.com", "Test123!");
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
    public async Task CascadeSoftDelete_DeleteCourse_ShouldSoftDeleteAllRelatedEntities()
    {
        // Arrange: Create course with lessons and exams
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _instructorToken);
        
        var createCourseRequest = new CreateCourseRequest("Test Course", "Description", false);
        var courseResponse = await _client.PostAsJsonAsync("/api/courses", createCourseRequest);
        var course = await courseResponse.Content.ReadFromJsonAsync<CourseResponse>();
        var courseId = course!.Id;

        // Create lesson 1 with exam
        var createLesson1Request = new CreateLessonRequest("Lesson 1", "Description", "Video", "https://example.com/video1.mp4", 1);
        var lesson1Response = await _client.PostAsJsonAsync($"/api/courses/{courseId}/lessons", createLesson1Request);
        var lesson1 = await lesson1Response.Content.ReadFromJsonAsync<LessonResponse>();
        var lesson1Id = lesson1!.Id;

        var createExam1Request = new CreateExamRequest(
            lesson1Id,
            "Exam 1",
            70,
            new List<CreateQuestionRequest>
            {
                new CreateQuestionRequest(
                    "Question 1?",
                    1,
                    new List<CreateChoiceRequest>
                    {
                        new CreateChoiceRequest("Answer 1", true),
                        new CreateChoiceRequest("Answer 2", false),
                        new CreateChoiceRequest("Answer 3", false)
                    }
                ),
                new CreateQuestionRequest(
                    "Question 2?",
                    2,
                    new List<CreateChoiceRequest>
                    {
                        new CreateChoiceRequest("Answer A", false),
                        new CreateChoiceRequest("Answer B", true)
                    }
                )
            }
        );
        
        var exam1Response = await _client.PostAsJsonAsync("/api/exams", createExam1Request);
        var exam1 = await exam1Response.Content.ReadFromJsonAsync<ExamResponse>();
        var exam1Id = exam1!.Id;

        // Create lesson 2 with exam
        var createLesson2Request = new CreateLessonRequest("Lesson 2", "Description", "PDF", "https://example.com/doc2.pdf", 2);
        var lesson2Response = await _client.PostAsJsonAsync($"/api/courses/{courseId}/lessons", createLesson2Request);
        var lesson2 = await lesson2Response.Content.ReadFromJsonAsync<LessonResponse>();
        var lesson2Id = lesson2!.Id;

        var createExam2Request = new CreateExamRequest(
            lesson2Id,
            "Exam 2",
            80,
            new List<CreateQuestionRequest>
            {
                new CreateQuestionRequest(
                    "Question 3?",
                    1,
                    new List<CreateChoiceRequest>
                    {
                        new CreateChoiceRequest("Correct", true),
                        new CreateChoiceRequest("Wrong", false)
                    }
                )
            }
        );
        
        var exam2Response = await _client.PostAsJsonAsync("/api/exams", createExam2Request);
        var exam2 = await exam2Response.Content.ReadFromJsonAsync<ExamResponse>();
        var exam2Id = exam2!.Id;

        // Act: Soft delete the course
        var deleteResponse = await _client.DeleteAsync($"/api/courses/{courseId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Assert: Verify course is soft deleted
        var courseEntity = await _dbContext.Courses.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == courseId);
        courseEntity.Should().NotBeNull();
        courseEntity!.IsDeleted.Should().BeTrue();
        courseEntity.DeletedAt.Should().NotBeNull();

        // Assert: Verify lessons are soft deleted
        var lesson1Entity = await _dbContext.Lessons.IgnoreQueryFilters().FirstOrDefaultAsync(l => l.Id == lesson1Id);
        lesson1Entity.Should().NotBeNull();
        lesson1Entity!.IsDeleted.Should().BeTrue();
        lesson1Entity.DeletedAt.Should().NotBeNull();

        var lesson2Entity = await _dbContext.Lessons.IgnoreQueryFilters().FirstOrDefaultAsync(l => l.Id == lesson2Id);
        lesson2Entity.Should().NotBeNull();
        lesson2Entity!.IsDeleted.Should().BeTrue();
        lesson2Entity.DeletedAt.Should().NotBeNull();

        // Assert: Verify exams are soft deleted
        var exam1Entity = await _dbContext.Exams.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == exam1Id);
        exam1Entity.Should().NotBeNull();
        exam1Entity!.IsDeleted.Should().BeTrue();
        exam1Entity.DeletedAt.Should().NotBeNull();

        var exam2Entity = await _dbContext.Exams.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == exam2Id);
        exam2Entity.Should().NotBeNull();
        exam2Entity!.IsDeleted.Should().BeTrue();
        exam2Entity.DeletedAt.Should().NotBeNull();

        // Assert: Verify questions are soft deleted
        var questions = await _dbContext.Questions.IgnoreQueryFilters()
            .Where(q => q.ExamId == exam1Id || q.ExamId == exam2Id)
            .ToListAsync();
        
        questions.Should().HaveCount(3);
        questions.Should().AllSatisfy(q =>
        {
            q.IsDeleted.Should().BeTrue();
            q.DeletedAt.Should().NotBeNull();
        });

        // Assert: Verify choices are soft deleted
        var questionIds = questions.Select(q => q.Id).ToList();
        var choices = await _dbContext.Choices.IgnoreQueryFilters()
            .Where(c => questionIds.Contains(c.QuestionId))
            .ToListAsync();
        
        choices.Should().HaveCount(7); // 3 + 2 + 2 = 7 choices total
        choices.Should().AllSatisfy(c =>
        {
            c.IsDeleted.Should().BeTrue();
            c.DeletedAt.Should().NotBeNull();
        });
    }

    [Fact]
    public async Task CascadeSoftDelete_DeleteLesson_ShouldSoftDeleteExamAndRelatedEntities()
    {
        // Arrange: Create course with lesson and exam
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _instructorToken);
        
        var createCourseRequest = new CreateCourseRequest("Test Course", "Description", false);
        var courseResponse = await _client.PostAsJsonAsync("/api/courses", createCourseRequest);
        var course = await courseResponse.Content.ReadFromJsonAsync<CourseResponse>();
        var courseId = course!.Id;

        var createLessonRequest = new CreateLessonRequest("Lesson 1", "Description", "Video", "https://example.com/video.mp4", 1);
        var lessonResponse = await _client.PostAsJsonAsync($"/api/courses/{courseId}/lessons", createLessonRequest);
        var lesson = await lessonResponse.Content.ReadFromJsonAsync<LessonResponse>();
        var lessonId = lesson!.Id;

        var createExamRequest = new CreateExamRequest(
            lessonId,
            "Exam 1",
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
        var exam = await examResponse.Content.ReadFromJsonAsync<ExamResponse>();
        var examId = exam!.Id;

        // Act: Soft delete the lesson
        var deleteResponse = await _client.DeleteAsync($"/api/courses/{courseId}/lessons/{lessonId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Assert: Verify lesson is soft deleted
        var lessonEntity = await _dbContext.Lessons.IgnoreQueryFilters().FirstOrDefaultAsync(l => l.Id == lessonId);
        lessonEntity.Should().NotBeNull();
        lessonEntity!.IsDeleted.Should().BeTrue();

        // Assert: Verify exam is soft deleted
        var examEntity = await _dbContext.Exams.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == examId);
        examEntity.Should().NotBeNull();
        examEntity!.IsDeleted.Should().BeTrue();

        // Assert: Verify questions and choices are soft deleted
        var questions = await _dbContext.Questions.IgnoreQueryFilters()
            .Where(q => q.ExamId == examId)
            .ToListAsync();
        
        questions.Should().AllSatisfy(q => q.IsDeleted.Should().BeTrue());

        var questionIds = questions.Select(q => q.Id).ToList();
        var choices = await _dbContext.Choices.IgnoreQueryFilters()
            .Where(c => questionIds.Contains(c.QuestionId))
            .ToListAsync();
        
        choices.Should().AllSatisfy(c => c.IsDeleted.Should().BeTrue());

        // Assert: Verify course is NOT soft deleted
        var courseEntity = await _dbContext.Courses.FirstOrDefaultAsync(c => c.Id == courseId);
        courseEntity.Should().NotBeNull();
        courseEntity!.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task CascadeSoftDelete_DeleteExam_ShouldSoftDeleteQuestionsAndChoices()
    {
        // Arrange: Create course, lesson, and exam
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _instructorToken);
        
        var createCourseRequest = new CreateCourseRequest("Test Course", "Description", false);
        var courseResponse = await _client.PostAsJsonAsync("/api/courses", createCourseRequest);
        var course = await courseResponse.Content.ReadFromJsonAsync<CourseResponse>();
        var courseId = course!.Id;

        var createLessonRequest = new CreateLessonRequest("Lesson 1", "Description", "Video", "https://example.com/video.mp4", 1);
        var lessonResponse = await _client.PostAsJsonAsync($"/api/courses/{courseId}/lessons", createLessonRequest);
        var lesson = await lessonResponse.Content.ReadFromJsonAsync<LessonResponse>();
        var lessonId = lesson!.Id;

        var createExamRequest = new CreateExamRequest(
            lessonId,
            "Exam 1",
            70,
            new List<CreateQuestionRequest>
            {
                new CreateQuestionRequest(
                    "Question 1?",
                    1,
                    new List<CreateChoiceRequest>
                    {
                        new CreateChoiceRequest("Answer 1", true),
                        new CreateChoiceRequest("Answer 2", false),
                        new CreateChoiceRequest("Answer 3", false)
                    }
                )
            }
        );
        
        var examResponse = await _client.PostAsJsonAsync("/api/exams", createExamRequest);
        var exam = await examResponse.Content.ReadFromJsonAsync<ExamResponse>();
        var examId = exam!.Id;

        // Act: Soft delete the exam
        var deleteResponse = await _client.DeleteAsync($"/api/exams/{examId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Assert: Verify exam is soft deleted
        var examEntity = await _dbContext.Exams.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == examId);
        examEntity.Should().NotBeNull();
        examEntity!.IsDeleted.Should().BeTrue();

        // Assert: Verify questions are soft deleted
        var questions = await _dbContext.Questions.IgnoreQueryFilters()
            .Where(q => q.ExamId == examId)
            .ToListAsync();
        
        questions.Should().HaveCount(1);
        questions.Should().AllSatisfy(q => q.IsDeleted.Should().BeTrue());

        // Assert: Verify choices are soft deleted
        var questionIds = questions.Select(q => q.Id).ToList();
        var choices = await _dbContext.Choices.IgnoreQueryFilters()
            .Where(c => questionIds.Contains(c.QuestionId))
            .ToListAsync();
        
        choices.Should().HaveCount(3);
        choices.Should().AllSatisfy(c => c.IsDeleted.Should().BeTrue());

        // Assert: Verify lesson and course are NOT soft deleted
        var lessonEntity = await _dbContext.Lessons.FirstOrDefaultAsync(l => l.Id == lessonId);
        lessonEntity.Should().NotBeNull();
        lessonEntity!.IsDeleted.Should().BeFalse();

        var courseEntity = await _dbContext.Courses.FirstOrDefaultAsync(c => c.Id == courseId);
        courseEntity.Should().NotBeNull();
        courseEntity!.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task CascadeSoftDelete_SoftDeletedEntities_ShouldNotAppearInDefaultQueries()
    {
        // Arrange: Create and delete a course
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _instructorToken);
        
        var createCourseRequest = new CreateCourseRequest("Test Course", "Description", false);
        var courseResponse = await _client.PostAsJsonAsync("/api/courses", createCourseRequest);
        var course = await courseResponse.Content.ReadFromJsonAsync<CourseResponse>();
        var courseId = course!.Id;

        await _client.DeleteAsync($"/api/courses/{courseId}");

        // Act: Try to retrieve the soft-deleted course
        var getResponse = await _client.GetAsync($"/api/courses/{courseId}");

        // Assert: Should return 404 (excluded by global query filter)
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Act: Query all courses
        var listResponse = await _client.GetAsync("/api/courses?pageNumber=1&pageSize=20");
        var courseList = await listResponse.Content.ReadFromJsonAsync<CourseListResponse>();

        // Assert: Soft-deleted course should not appear in list
        courseList.Should().NotBeNull();
        courseList!.Courses.Should().NotContain(c => c.Id == courseId);
    }
}
