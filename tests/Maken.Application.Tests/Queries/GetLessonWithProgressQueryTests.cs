using Maken.Application.Common.Interfaces;
using Maken.Application.Queries.Lessons;
using Maken.Application.Tests.Helpers;
using Maken.Domain.Entities;
using Maken.Domain.Enums;
using Maken.Domain.Services;
using Moq;

namespace Maken.Application.Tests.Queries;

/// <summary>
/// Unit tests for GetLessonWithProgressQueryHandler.
/// Tests lesson retrieval with progress tracking and lock status.
/// </summary>
public class GetLessonWithProgressQueryTests
{
    private readonly Mock<IRepository<Lesson>> _lessonRepositoryMock;
    private readonly Mock<ICourseRepository> _courseRepositoryMock;
    private readonly Mock<IProgressRepository> _progressRepositoryMock;
    private readonly ProgressionService _progressionService;
    private readonly GetLessonWithProgressQueryHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _studentId = Guid.NewGuid();
    private readonly Guid _courseId = Guid.NewGuid();

    public GetLessonWithProgressQueryTests()
    {
        _lessonRepositoryMock = new Mock<IRepository<Lesson>>();
        _courseRepositoryMock = new Mock<ICourseRepository>();
        _progressRepositoryMock = new Mock<IProgressRepository>();
        _progressionService = new ProgressionService();

        _handler = new GetLessonWithProgressQueryHandler(
            _lessonRepositoryMock.Object,
            _courseRepositoryMock.Object,
            _progressRepositoryMock.Object,
            _progressionService
        );
    }

    [Fact]
    public async Task Handle_WithValidIds_ReturnsLesson()
    {
        // Arrange
        var lessonId = Guid.NewGuid();
        var lesson = Lesson.Create(_courseId, _tenantId, "Test Lesson", "Description", ContentType.Video, "http://example.com/video", 1);
        EntityTestHelper.SetId(lesson, lessonId);

        var course = Course.Create("Test Course", "Description", _tenantId);
        EntityTestHelper.SetId(course, _courseId);

        _lessonRepositoryMock
            .Setup(x => x.GetByIdAsync(lessonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lesson);

        _courseRepositoryMock
            .Setup(x => x.GetByIdAsync(_courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(course);

        _progressRepositoryMock
            .Setup(x => x.GetByStudentAndLessonAsync(_studentId, lessonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Progress?)null);

        _progressRepositoryMock
            .Setup(x => x.GetByStudentAndCourseAsync(_studentId, _courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Progress>());

        var query = new GetLessonWithProgressQuery(_studentId, _courseId, lessonId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(lessonId, result.Id);
        Assert.Equal("Test Lesson", result.Title);
        Assert.Equal("Description", result.Description);
    }

    [Fact]
    public async Task Handle_WithNonExistentLesson_ReturnsNull()
    {
        // Arrange
        var lessonId = Guid.NewGuid();
        _lessonRepositoryMock
            .Setup(x => x.GetByIdAsync(lessonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Lesson?)null);

        var query = new GetLessonWithProgressQuery(_studentId, _courseId, lessonId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_WithNonExistentCourse_ReturnsNull()
    {
        // Arrange
        var lessonId = Guid.NewGuid();
        var lesson = Lesson.Create(_courseId, _tenantId, "Test Lesson", "Description", ContentType.Video, "http://example.com/video", 1);
        EntityTestHelper.SetId(lesson, lessonId);

        _lessonRepositoryMock
            .Setup(x => x.GetByIdAsync(lessonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lesson);

        _courseRepositoryMock
            .Setup(x => x.GetByIdAsync(_courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Course?)null);

        var query = new GetLessonWithProgressQuery(_studentId, _courseId, lessonId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_WithFirstLesson_ReturnsUnlockedLesson()
    {
        // Arrange
        var lessonId = Guid.NewGuid();
        var lesson = Lesson.Create(_courseId, _tenantId, "First Lesson", "Description", ContentType.Video, "http://example.com/video", 1);
        EntityTestHelper.SetId(lesson, lessonId);

        var course = Course.Create("Test Course", "Description", _tenantId);
        EntityTestHelper.SetId(course, _courseId);

        _lessonRepositoryMock
            .Setup(x => x.GetByIdAsync(lessonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lesson);

        _courseRepositoryMock
            .Setup(x => x.GetByIdAsync(_courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(course);

        _progressRepositoryMock
            .Setup(x => x.GetByStudentAndLessonAsync(_studentId, lessonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Progress?)null);

        _progressRepositoryMock
            .Setup(x => x.GetByStudentAndCourseAsync(_studentId, _courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Progress>());

        var query = new GetLessonWithProgressQuery(_studentId, _courseId, lessonId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsLocked);
    }

    [Fact]
    public async Task Handle_WithSecondLessonAndPreviousCompleted_ReturnsUnlockedLesson()
    {
        // Arrange
        var lesson1Id = Guid.NewGuid();
        var lesson2Id = Guid.NewGuid();

        var lesson1 = Lesson.Create(_courseId, _tenantId, "Lesson 1", "Description", ContentType.Video, "http://example.com/video1", 1);
        EntityTestHelper.SetId(lesson1, lesson1Id);

        var lesson2 = Lesson.Create(_courseId, _tenantId, "Lesson 2", "Description", ContentType.Video, "http://example.com/video2", 2);
        EntityTestHelper.SetId(lesson2, lesson2Id);

        var course = Course.Create("Test Course", "Description", _tenantId);
        EntityTestHelper.SetId(course, _courseId);

        var progress1 = Progress.Create(_studentId, lesson1Id, _tenantId);
        progress1.MarkAsCompleted();

        _lessonRepositoryMock
            .Setup(x => x.GetByIdAsync(lesson2Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lesson2);

        _courseRepositoryMock
            .Setup(x => x.GetByIdAsync(_courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(course);

        // Mock GetByIdWithLessonsAsync to return course with lessons
        var courseWithLessons = Course.Create("Test Course", "Description", _tenantId);
        EntityTestHelper.SetId(courseWithLessons, _courseId);
        // Use reflection to set the Lessons collection
        var lessonsProperty = typeof(Course).GetProperty("Lessons");
        lessonsProperty?.SetValue(courseWithLessons, new List<Lesson> { lesson1, lesson2 });

        _courseRepositoryMock
            .Setup(x => x.GetByIdWithLessonsAsync(_courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(courseWithLessons);

        _progressRepositoryMock
            .Setup(x => x.GetByStudentAndLessonAsync(_studentId, lesson2Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Progress?)null);

        _progressRepositoryMock
            .Setup(x => x.GetByStudentAndCourseAsync(_studentId, _courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Progress> { progress1 });

        var query = new GetLessonWithProgressQuery(_studentId, _courseId, lesson2Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsLocked);
    }

    [Fact]
    public async Task Handle_WithSecondLessonAndPreviousNotCompleted_ReturnsLockedLesson()
    {
        // Arrange
        var lesson1Id = Guid.NewGuid();
        var lesson2Id = Guid.NewGuid();

        var lesson1 = Lesson.Create(_courseId, _tenantId, "Lesson 1", "Description", ContentType.Video, "http://example.com/video1", 1);
        EntityTestHelper.SetId(lesson1, lesson1Id);

        var lesson2 = Lesson.Create(_courseId, _tenantId, "Lesson 2", "Description", ContentType.Video, "http://example.com/video2", 2);
        EntityTestHelper.SetId(lesson2, lesson2Id);

        var course = Course.Create("Test Course", "Description", _tenantId);
        EntityTestHelper.SetId(course, _courseId);
        // Use reflection to set the Lessons collection
        var lessonsProperty = typeof(Course).GetProperty("Lessons");
        lessonsProperty?.SetValue(course, new List<Lesson> { lesson1, lesson2 });

        var progress1 = Progress.Create(_studentId, lesson1Id, _tenantId);
        // Not marked as completed

        _lessonRepositoryMock
            .Setup(x => x.GetByIdAsync(lesson2Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lesson2);

        _courseRepositoryMock
            .Setup(x => x.GetByIdAsync(_courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(course);

        _courseRepositoryMock
            .Setup(x => x.GetByIdWithLessonsAsync(_courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(course);

        _progressRepositoryMock
            .Setup(x => x.GetByStudentAndLessonAsync(_studentId, lesson2Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Progress?)null);

        _progressRepositoryMock
            .Setup(x => x.GetByStudentAndCourseAsync(_studentId, _courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Progress> { progress1 });

        var query = new GetLessonWithProgressQuery(_studentId, _courseId, lesson2Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsLocked);
    }

    [Fact]
    public async Task Handle_WithFreeFlowMode_ReturnsUnlockedLesson()
    {
        // Arrange
        var lessonId = Guid.NewGuid();
        var lesson = Lesson.Create(_courseId, _tenantId, "Lesson 2", "Description", ContentType.Video, "http://example.com/video", 2);
        EntityTestHelper.SetId(lesson, lessonId);

        var course = Course.Create("Test Course", "Description", _tenantId);
        EntityTestHelper.SetId(course, _courseId);
        course.SetFreeFlowMode(true);

        _lessonRepositoryMock
            .Setup(x => x.GetByIdAsync(lessonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lesson);

        _courseRepositoryMock
            .Setup(x => x.GetByIdAsync(_courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(course);

        _progressRepositoryMock
            .Setup(x => x.GetByStudentAndLessonAsync(_studentId, lessonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Progress?)null);

        _progressRepositoryMock
            .Setup(x => x.GetByStudentAndCourseAsync(_studentId, _courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Progress>());

        var query = new GetLessonWithProgressQuery(_studentId, _courseId, lessonId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsLocked);
    }

    [Fact]
    public async Task Handle_WithCompletedLesson_ReturnsCompletedStatus()
    {
        // Arrange
        var lessonId = Guid.NewGuid();
        var lesson = Lesson.Create(_courseId, _tenantId, "Test Lesson", "Description", ContentType.Video, "http://example.com/video", 1);
        EntityTestHelper.SetId(lesson, lessonId);

        var course = Course.Create("Test Course", "Description", _tenantId);
        EntityTestHelper.SetId(course, _courseId);

        var progress = Progress.Create(_studentId, lessonId, _tenantId);
        progress.MarkAsCompleted();

        _lessonRepositoryMock
            .Setup(x => x.GetByIdAsync(lessonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lesson);

        _courseRepositoryMock
            .Setup(x => x.GetByIdAsync(_courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(course);

        _progressRepositoryMock
            .Setup(x => x.GetByStudentAndLessonAsync(_studentId, lessonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(progress);

        _progressRepositoryMock
            .Setup(x => x.GetByStudentAndCourseAsync(_studentId, _courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Progress>());

        var query = new GetLessonWithProgressQuery(_studentId, _courseId, lessonId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsCompleted);
    }
}
