using Maken.Application.Common.Interfaces;
using Maken.Domain.Entities;
using Maken.Domain.Enums;
using Maken.Infrastructure.Persistence;
using Maken.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Maken.Infrastructure.Tests.Repositories;

/// <summary>
/// Unit tests for ProgressRepository.
/// Tests progress-specific query methods and tenant isolation.
/// </summary>
public class ProgressRepositoryTests : IDisposable
{
    private readonly MakenDbContext _context;
    private readonly ProgressRepository _repository;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _studentId = Guid.NewGuid();

    public ProgressRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<MakenDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.TenantId).Returns(_tenantId);

        _context = new MakenDbContext(options, mockTenantContext.Object);
        _repository = new ProgressRepository(_context);
    }

    [Fact]
    public async Task GetByStudentAndLessonAsync_WithValidIds_ReturnsProgress()
    {
        // Arrange
        var course = Course.Create("Test Course", "Test Description", _tenantId);
        await _context.Courses.AddAsync(course);
        await _context.SaveChangesAsync();

        var lesson = Lesson.Create(course.Id, _tenantId, "Test Lesson", "Description", ContentType.Video, "http://example.com/video", 1);
        await _context.Lessons.AddAsync(lesson);
        await _context.SaveChangesAsync();

        var progress = Progress.Create(_studentId, lesson.Id, _tenantId);
        await _context.Progresses.AddAsync(progress);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByStudentAndLessonAsync(_studentId, lesson.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_studentId, result.StudentId);
        Assert.Equal(lesson.Id, result.LessonId);
    }

    [Fact]
    public async Task GetByStudentAndLessonAsync_WithInvalidIds_ReturnsNull()
    {
        // Arrange
        var invalidStudentId = Guid.NewGuid();
        var invalidLessonId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByStudentAndLessonAsync(invalidStudentId, invalidLessonId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByStudentAndCourseAsync_WithValidIds_ReturnsAllProgressForCourse()
    {
        // Arrange
        var course = Course.Create("Test Course", "Test Description", _tenantId);
        await _context.Courses.AddAsync(course);
        await _context.SaveChangesAsync();

        var lesson1 = Lesson.Create(course.Id, _tenantId, "Lesson 1", "Description 1", ContentType.Video, "http://example.com/video1", 1);
        var lesson2 = Lesson.Create(course.Id, _tenantId, "Lesson 2", "Description 2", ContentType.PDF, "http://example.com/pdf1", 2);
        await _context.Lessons.AddRangeAsync(lesson1, lesson2);
        await _context.SaveChangesAsync();

        var progress1 = Progress.Create(_studentId, lesson1.Id, _tenantId);
        var progress2 = Progress.Create(_studentId, lesson2.Id, _tenantId);
        await _context.Progresses.AddRangeAsync(progress1, progress2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByStudentAndCourseAsync(_studentId, course.Id);
        var resultList = result.ToList();

        // Assert
        Assert.Equal(2, resultList.Count);
        Assert.All(resultList, p => Assert.Equal(_studentId, p.StudentId));
    }

    [Fact]
    public async Task GetByStudentAndCourseAsync_WithDifferentCourse_ReturnsEmpty()
    {
        // Arrange
        var course1 = Course.Create("Course 1", "Description 1", _tenantId);
        var course2 = Course.Create("Course 2", "Description 2", _tenantId);
        await _context.Courses.AddRangeAsync(course1, course2);
        await _context.SaveChangesAsync();

        var lesson1 = Lesson.Create(course1.Id, _tenantId, "Lesson 1", "Description 1", ContentType.Video, "http://example.com/video1", 1);
        await _context.Lessons.AddAsync(lesson1);
        await _context.SaveChangesAsync();

        var progress1 = Progress.Create(_studentId, lesson1.Id, _tenantId);
        await _context.Progresses.AddAsync(progress1);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByStudentAndCourseAsync(_studentId, course2.Id);
        var resultList = result.ToList();

        // Assert
        Assert.Empty(resultList);
    }

    [Fact]
    public async Task GetByStudentAndCourseAsync_IncludesLessonNavigation()
    {
        // Arrange
        var course = Course.Create("Test Course", "Test Description", _tenantId);
        await _context.Courses.AddAsync(course);
        await _context.SaveChangesAsync();

        var lesson = Lesson.Create(course.Id, _tenantId, "Test Lesson", "Description", ContentType.Video, "http://example.com/video", 1);
        await _context.Lessons.AddAsync(lesson);
        await _context.SaveChangesAsync();

        var progress = Progress.Create(_studentId, lesson.Id, _tenantId);
        await _context.Progresses.AddAsync(progress);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByStudentAndCourseAsync(_studentId, course.Id);
        var resultList = result.ToList();

        // Assert
        Assert.Single(resultList);
        Assert.NotNull(resultList[0].Lesson);
        Assert.Equal("Test Lesson", resultList[0].Lesson!.Title);
    }

    [Fact]
    public async Task AddAsync_WithValidProgress_AddsProgress()
    {
        // Arrange
        var course = Course.Create("Test Course", "Test Description", _tenantId);
        await _context.Courses.AddAsync(course);
        await _context.SaveChangesAsync();

        var lesson = Lesson.Create(course.Id, _tenantId, "Test Lesson", "Description", ContentType.Video, "http://example.com/video", 1);
        await _context.Lessons.AddAsync(lesson);
        await _context.SaveChangesAsync();

        var progress = Progress.Create(_studentId, lesson.Id, _tenantId);

        // Act
        await _repository.AddAsync(progress);
        await _context.SaveChangesAsync();

        // Assert
        var result = await _context.Progresses.FindAsync(progress.Id);
        Assert.NotNull(result);
        Assert.Equal(_studentId, result.StudentId);
        Assert.Equal(lesson.Id, result.LessonId);
    }

    [Fact]
    public async Task UpdateAsync_WithModifiedProgress_UpdatesProgress()
    {
        // Arrange
        var course = Course.Create("Test Course", "Test Description", _tenantId);
        await _context.Courses.AddAsync(course);
        await _context.SaveChangesAsync();

        var lesson = Lesson.Create(course.Id, _tenantId, "Test Lesson", "Description", ContentType.Video, "http://example.com/video", 1);
        await _context.Lessons.AddAsync(lesson);
        await _context.SaveChangesAsync();

        var progress = Progress.Create(_studentId, lesson.Id, _tenantId);
        await _context.Progresses.AddAsync(progress);
        await _context.SaveChangesAsync();

        // Act
        progress.MarkAsCompleted();
        _repository.Update(progress);
        await _context.SaveChangesAsync();

        // Assert
        var result = await _context.Progresses.FindAsync(progress.Id);
        Assert.NotNull(result);
        Assert.NotNull(result.CompletedAt);
    }

    [Fact]
    public async Task GetAllAsync_ExcludesSoftDeletedProgress()
    {
        // Arrange
        var course = Course.Create("Test Course", "Test Description", _tenantId);
        await _context.Courses.AddAsync(course);
        await _context.SaveChangesAsync();

        var lesson = Lesson.Create(course.Id, _tenantId, "Test Lesson", "Description", ContentType.Video, "http://example.com/video", 1);
        await _context.Lessons.AddAsync(lesson);
        await _context.SaveChangesAsync();

        var activeProgress = Progress.Create(_studentId, lesson.Id, _tenantId);
        var deletedProgress = Progress.Create(Guid.NewGuid(), lesson.Id, _tenantId);
        deletedProgress.SoftDelete();

        await _context.Progresses.AddRangeAsync(activeProgress, deletedProgress);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal(activeProgress.Id, result.First().Id);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
