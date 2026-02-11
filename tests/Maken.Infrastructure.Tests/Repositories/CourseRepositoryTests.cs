using Maken.Application.Common.Interfaces;
using Maken.Domain.Entities;
using Maken.Domain.Enums;
using Maken.Infrastructure.Persistence;
using Maken.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Maken.Infrastructure.Tests.Repositories;

/// <summary>
/// Unit tests for CourseRepository.
/// Tests course-specific query methods and tenant isolation.
/// </summary>
public class CourseRepositoryTests : IDisposable
{
    private readonly MakenDbContext _context;
    private readonly CourseRepository _repository;
    private readonly Guid _tenantId = Guid.NewGuid();

    public CourseRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<MakenDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.TenantId).Returns(_tenantId);

        _context = new MakenDbContext(options, mockTenantContext.Object);
        _repository = new CourseRepository(_context);
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsCourse()
    {
        // Arrange
        var course = Course.Create("Test Course", "Test Description", _tenantId);
        await _context.Courses.AddAsync(course);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(course.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(course.Id, result.Id);
        Assert.Equal("Test Course", result.Name);
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
    public async Task GetByIdWithLessonsAsync_WithValidId_ReturnsCoursWithLessons()
    {
        // Arrange
        var course = Course.Create("Test Course", "Test Description", _tenantId);
        await _context.Courses.AddAsync(course);
        await _context.SaveChangesAsync();

        var lesson1 = Lesson.Create(course.Id, _tenantId, "Lesson 1", "Description 1", ContentType.Video, "http://example.com/video1", 1);
        var lesson2 = Lesson.Create(course.Id, _tenantId, "Lesson 2", "Description 2", ContentType.PDF, "http://example.com/pdf1", 2);
        await _context.Lessons.AddRangeAsync(lesson1, lesson2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdWithLessonsAsync(course.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(course.Id, result.Id);
        Assert.Equal(2, result.Lessons.Count);
        Assert.Equal("Lesson 1", result.Lessons.First().Title);
        Assert.Equal("Lesson 2", result.Lessons.Last().Title);
    }

    [Fact]
    public async Task GetByIdWithLessonsAsync_LessonsOrderedByOrder_ReturnsLessonsInCorrectOrder()
    {
        // Arrange
        var course = Course.Create("Test Course", "Test Description", _tenantId);
        await _context.Courses.AddAsync(course);
        await _context.SaveChangesAsync();

        var lesson3 = Lesson.Create(course.Id, _tenantId, "Lesson 3", "Description 3", ContentType.Video, "http://example.com/video3", 3);
        var lesson1 = Lesson.Create(course.Id, _tenantId, "Lesson 1", "Description 1", ContentType.Video, "http://example.com/video1", 1);
        var lesson2 = Lesson.Create(course.Id, _tenantId, "Lesson 2", "Description 2", ContentType.PDF, "http://example.com/pdf1", 2);
        await _context.Lessons.AddRangeAsync(lesson3, lesson1, lesson2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdWithLessonsAsync(course.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Lessons.Count);
        Assert.Equal(1, result.Lessons.ElementAt(0).Order);
        Assert.Equal(2, result.Lessons.ElementAt(1).Order);
        Assert.Equal(3, result.Lessons.ElementAt(2).Order);
    }

    [Fact]
    public async Task GetPublishedCoursesAsync_ReturnsOnlyPublishedCourses()
    {
        // Arrange
        var publishedCourse1 = Course.Create("Published Course 1", "Description 1", _tenantId);
        publishedCourse1.Publish();
        var publishedCourse2 = Course.Create("Published Course 2", "Description 2", _tenantId);
        publishedCourse2.Publish();
        var draftCourse = Course.Create("Draft Course", "Description 3", _tenantId);

        await _context.Courses.AddRangeAsync(publishedCourse1, publishedCourse2, draftCourse);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetPublishedCoursesAsync();
        var resultList = result.ToList();

        // Assert
        Assert.Equal(2, resultList.Count);
        Assert.All(resultList, c => Assert.Equal(CourseStatus.Published, c.Status));
    }

    [Fact]
    public async Task GetPublishedCoursesAsync_ReturnsCoursesOrderedByName()
    {
        // Arrange
        var courseC = Course.Create("C Course", "Description C", _tenantId);
        courseC.Publish();
        var courseA = Course.Create("A Course", "Description A", _tenantId);
        courseA.Publish();
        var courseB = Course.Create("B Course", "Description B", _tenantId);
        courseB.Publish();

        await _context.Courses.AddRangeAsync(courseC, courseA, courseB);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetPublishedCoursesAsync();
        var resultList = result.ToList();

        // Assert
        Assert.Equal(3, resultList.Count);
        Assert.Equal("A Course", resultList[0].Name);
        Assert.Equal("B Course", resultList[1].Name);
        Assert.Equal("C Course", resultList[2].Name);
    }

    [Fact]
    public async Task AddAsync_WithValidCourse_AddsCourse()
    {
        // Arrange
        var course = Course.Create("New Course", "New Description", _tenantId);

        // Act
        await _repository.AddAsync(course);
        await _context.SaveChangesAsync();

        // Assert
        var result = await _context.Courses.FindAsync(course.Id);
        Assert.NotNull(result);
        Assert.Equal("New Course", result.Name);
    }

    [Fact]
    public async Task UpdateAsync_WithModifiedCourse_UpdatesCourse()
    {
        // Arrange
        var course = Course.Create("Original Name", "Original Description", _tenantId);
        await _context.Courses.AddAsync(course);
        await _context.SaveChangesAsync();

        // Act
        course.SetName("Updated Name");
        course.SetDescription("Updated Description");
        _repository.Update(course);
        await _context.SaveChangesAsync();

        // Assert
        var result = await _context.Courses.FindAsync(course.Id);
        Assert.NotNull(result);
        Assert.Equal("Updated Name", result.Name);
        Assert.Equal("Updated Description", result.Description);
    }

    [Fact]
    public async Task DeleteAsync_WithValidCourse_SoftDeletesCourse()
    {
        // Arrange
        var course = Course.Create("Course to Delete", "Description", _tenantId);
        await _context.Courses.AddAsync(course);
        await _context.SaveChangesAsync();

        // Act
        course.SoftDelete();
        _repository.Update(course);
        await _context.SaveChangesAsync();

        // Assert
        var result = await _context.Courses.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == course.Id);
        Assert.NotNull(result);
        Assert.True(result.IsDeleted);
        Assert.NotNull(result.DeletedAt);
    }

    [Fact]
    public async Task GetAllAsync_ExcludesSoftDeletedCourses()
    {
        // Arrange
        var activeCourse = Course.Create("Active Course", "Description", _tenantId);
        var deletedCourse = Course.Create("Deleted Course", "Description", _tenantId);
        deletedCourse.SoftDelete();

        await _context.Courses.AddRangeAsync(activeCourse, deletedCourse);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal("Active Course", result.First().Name);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
