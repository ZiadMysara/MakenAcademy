using Maken.Application.Common.Interfaces;
using Maken.Domain.Entities;
using Maken.Domain.Enums;
using Maken.Infrastructure.Persistence;
using Maken.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Maken.Infrastructure.Tests.Repositories;

/// <summary>
/// Unit tests for LessonRepository.
/// Tests lesson-specific query methods and tenant isolation.
/// </summary>
public class LessonRepositoryTests : IDisposable
{
    private readonly MakenDbContext _context;
    private readonly LessonRepository _repository;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _courseId;

    public LessonRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<MakenDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.TenantId).Returns(_tenantId);

        _context = new MakenDbContext(options, mockTenantContext.Object);
        _repository = new LessonRepository(_context);

        // Create a course for testing
        var course = Course.Create("Test Course", "Test Description", _tenantId);
        _context.Courses.Add(course);
        _context.SaveChanges();
        _courseId = course.Id;
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsLesson()
    {
        // Arrange
        var lesson = Lesson.Create(_courseId, _tenantId, "Test Lesson", "Test Description", ContentType.Video, "http://example.com/video", 1);
        await _context.Lessons.AddAsync(lesson);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(lesson.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(lesson.Id, result.Id);
        Assert.Equal("Test Lesson", result.Title);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(invalidId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByCourseIdAsync_WithValidCourseId_ReturnsLessonsOrderedByOrder()
    {
        // Arrange
        var lesson3 = Lesson.Create(_courseId, _tenantId, "Lesson 3", "Description 3", ContentType.Video, "http://example.com/video3", 3);
        var lesson1 = Lesson.Create(_courseId, _tenantId, "Lesson 1", "Description 1", ContentType.Video, "http://example.com/video1", 1);
        var lesson2 = Lesson.Create(_courseId, _tenantId, "Lesson 2", "Description 2", ContentType.PDF, "http://example.com/pdf1", 2);
        await _context.Lessons.AddRangeAsync(lesson3, lesson1, lesson2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByCourseIdAsync(_courseId);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal(1, result[0].Order);
        Assert.Equal(2, result[1].Order);
        Assert.Equal(3, result[2].Order);
        Assert.Equal("Lesson 1", result[0].Title);
        Assert.Equal("Lesson 2", result[1].Title);
        Assert.Equal("Lesson 3", result[2].Title);
    }

    [Fact]
    public async Task GetByCourseIdAsync_WithNonExistentCourseId_ReturnsEmptyList()
    {
        // Arrange
        var nonExistentCourseId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByCourseIdAsync(nonExistentCourseId);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByIdWithExamAsync_WithLessonHavingExam_ReturnsLessonWithExam()
    {
        // Arrange
        var lesson = Lesson.Create(_courseId, _tenantId, "Test Lesson", "Test Description", ContentType.Video, "http://example.com/video", 1);
        await _context.Lessons.AddAsync(lesson);
        await _context.SaveChangesAsync();

        var exam = Exam.Create(lesson.Id, _tenantId, "Test Exam", 70);
        await _context.Exams.AddAsync(exam);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdWithExamAsync(lesson.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(lesson.Id, result.Id);
        Assert.NotNull(result.Exam);
        Assert.Equal("Test Exam", result.Exam.Title);
    }

    [Fact]
    public async Task GetByIdWithExamAsync_WithLessonWithoutExam_ReturnsLessonWithNullExam()
    {
        // Arrange
        var lesson = Lesson.Create(_courseId, _tenantId, "Test Lesson", "Test Description", ContentType.Video, "http://example.com/video", 1);
        await _context.Lessons.AddAsync(lesson);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdWithExamAsync(lesson.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(lesson.Id, result.Id);
        Assert.Null(result.Exam);
    }

    [Fact]
    public async Task AddAsync_WithValidLesson_AddsLesson()
    {
        // Arrange
        var lesson = Lesson.Create(_courseId, _tenantId, "New Lesson", "New Description", ContentType.Video, "http://example.com/video", 1);

        // Act
        await _repository.AddAsync(lesson);
        await _context.SaveChangesAsync();

        // Assert
        var result = await _context.Lessons.FindAsync(lesson.Id);
        Assert.NotNull(result);
        Assert.Equal("New Lesson", result.Title);
    }

    [Fact]
    public async Task UpdateAsync_WithModifiedLesson_UpdatesLesson()
    {
        // Arrange
        var lesson = Lesson.Create(_courseId, _tenantId, "Original Title", "Original Description", ContentType.Video, "http://example.com/video", 1);
        await _context.Lessons.AddAsync(lesson);
        await _context.SaveChangesAsync();

        // Act
        lesson.SetTitle("Updated Title");
        lesson.SetDescription("Updated Description");
        _repository.Update(lesson);
        await _context.SaveChangesAsync();

        // Assert
        var result = await _context.Lessons.FindAsync(lesson.Id);
        Assert.NotNull(result);
        Assert.Equal("Updated Title", result.Title);
        Assert.Equal("Updated Description", result.Description);
    }

    [Fact]
    public async Task DeleteAsync_WithValidLesson_SoftDeletesLesson()
    {
        // Arrange
        var lesson = Lesson.Create(_courseId, _tenantId, "Lesson to Delete", "Description", ContentType.Video, "http://example.com/video", 1);
        await _context.Lessons.AddAsync(lesson);
        await _context.SaveChangesAsync();

        // Act
        lesson.SoftDelete();
        _repository.Update(lesson);
        await _context.SaveChangesAsync();

        // Assert
        var result = await _context.Lessons.IgnoreQueryFilters().FirstOrDefaultAsync(l => l.Id == lesson.Id);
        Assert.NotNull(result);
        Assert.True(result.IsDeleted);
        Assert.NotNull(result.DeletedAt);
    }

    [Fact]
    public async Task GetAllAsync_ExcludesSoftDeletedLessons()
    {
        // Arrange
        var activeLesson = Lesson.Create(_courseId, _tenantId, "Active Lesson", "Description", ContentType.Video, "http://example.com/video", 1);
        var deletedLesson = Lesson.Create(_courseId, _tenantId, "Deleted Lesson", "Description", ContentType.Video, "http://example.com/video", 2);
        deletedLesson.SoftDelete();

        await _context.Lessons.AddRangeAsync(activeLesson, deletedLesson);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal("Active Lesson", result.First().Title);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
