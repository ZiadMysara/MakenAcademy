using Maken.Application.Common.Interfaces;
using Maken.Application.DTOs;
using Maken.Application.Queries.Courses;
using Maken.Domain.Entities;
using Maken.Domain.Enums;
using Maken.Domain.Services;
using Moq;

namespace Maken.Application.Tests.Queries;

public class GetStudentCoursesQueryTests
{
    private readonly Mock<ICourseRepository> _courseRepositoryMock;
    private readonly Mock<IEnrollmentRepository> _enrollmentRepositoryMock;
    private readonly Mock<IProgressRepository> _progressRepositoryMock;
    private readonly ProgressionService _progressionService;
    private readonly GetStudentCoursesQueryHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _studentId = Guid.NewGuid();

    public GetStudentCoursesQueryTests()
    {
        _courseRepositoryMock = new Mock<ICourseRepository>();
        _enrollmentRepositoryMock = new Mock<IEnrollmentRepository>();
        _progressRepositoryMock = new Mock<IProgressRepository>();
        _progressionService = new ProgressionService();

        _handler = new GetStudentCoursesQueryHandler(
            _courseRepositoryMock.Object,
            _enrollmentRepositoryMock.Object,
            _progressRepositoryMock.Object,
            _progressionService);
    }

    [Fact]
    public async Task Handle_WithNoPublishedCourses_ReturnsEmptyList()
    {
        // Arrange
        var query = new GetStudentCoursesQuery
        {
            StudentId = _studentId,
            PageNumber = 1,
            PageSize = 10
        };
        _courseRepositoryMock.Setup(x => x.GetPublishedCoursesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Course>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task Handle_WithUnenrolledStudent_ReturnsCoursesAsLocked()
    {
        // Arrange
        var course = Course.Create("Test Course", "Description", _tenantId);
        course.Publish();

        var query = new GetStudentCoursesQuery
        {
            StudentId = _studentId,
            PageNumber = 1,
            PageSize = 10
        };
        _courseRepositoryMock.Setup(x => x.GetPublishedCoursesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Course> { course });
        _enrollmentRepositoryMock.Setup(x => x.GetByStudentIdAsync(_studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Enrollment>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);
        var resultList = result.ToList();

        // Assert
        Assert.Single(resultList);
        Assert.True(resultList[0].IsLocked);
        Assert.Equal(0, resultList[0].CompletionPercentage);
    }

    [Fact]
    public async Task Handle_WithEnrolledStudentNoPrerequisites_ReturnsUnlockedCourse()
    {
        // Arrange
        var course = Course.Create("Test Course", "Description", _tenantId);
        course.Publish();

        var enrollment = new Enrollment
        {
            TenantId = _tenantId,
            StudentId = _studentId,
            CourseId = course.Id,
            EnrolledAt = DateTime.UtcNow
        };

        var query = new GetStudentCoursesQuery
        {
            StudentId = _studentId,
            PageNumber = 1,
            PageSize = 10
        };
        _courseRepositoryMock.Setup(x => x.GetPublishedCoursesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Course> { course });
        _enrollmentRepositoryMock.Setup(x => x.GetByStudentIdAsync(_studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Enrollment> { enrollment });
        _progressRepositoryMock.Setup(x => x.GetByStudentAndCourseAsync(_studentId, course.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Progress>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);
        var resultList = result.ToList();

        // Assert
        Assert.Single(resultList);
        Assert.False(resultList[0].IsLocked);
        Assert.Equal(0, resultList[0].CompletionPercentage);
    }

    [Fact]
    public async Task Handle_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var courses = new List<Course>();
        for (int i = 0; i < 25; i++)
        {
            var course = Course.Create($"Course {i}", $"Description {i}", _tenantId);
            course.Publish();
            courses.Add(course);
        }

        var query = new GetStudentCoursesQuery
        {
            StudentId = _studentId,
            PageNumber = 2,
            PageSize = 10
        };
        _courseRepositoryMock.Setup(x => x.GetPublishedCoursesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(courses);
        _enrollmentRepositoryMock.Setup(x => x.GetByStudentIdAsync(_studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Enrollment>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);
        var resultList = result.ToList();

        // Assert
        Assert.Equal(10, resultList.Count);
    }

    [Fact]
    public async Task Handle_WithCompletedLessons_CalculatesCompletionPercentage()
    {
        // Arrange
        var course = Course.Create("Test Course", "Description", _tenantId);
        course.Publish();

        var lesson1 = Lesson.Create(course.Id, _tenantId, "Lesson 1", "Desc", ContentType.Video, "url", 1);
        var lesson2 = Lesson.Create(course.Id, _tenantId, "Lesson 2", "Desc", ContentType.Video, "url", 2);
        course.GetType().GetProperty("Lessons")!.SetValue(course, new List<Lesson> { lesson1, lesson2 });

        var enrollment = new Enrollment
        {
            TenantId = _tenantId,
            StudentId = _studentId,
            CourseId = course.Id,
            EnrolledAt = DateTime.UtcNow
        };

        var progress1 = CreateProgress(_studentId, lesson1.Id, _tenantId, completed: true, examPassed: true);
        var progressList = new List<Progress> { progress1 };

        var query = new GetStudentCoursesQuery
        {
            StudentId = _studentId,
            PageNumber = 1,
            PageSize = 10
        };
        _courseRepositoryMock.Setup(x => x.GetPublishedCoursesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Course> { course });
        _enrollmentRepositoryMock.Setup(x => x.GetByStudentIdAsync(_studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Enrollment> { enrollment });
        _progressRepositoryMock.Setup(x => x.GetByStudentAndCourseAsync(_studentId, course.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(progressList);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);
        var resultList = result.ToList();

        // Assert
        Assert.Single(resultList);
        Assert.Equal(50, resultList[0].CompletionPercentage);
    }

    private Progress CreateProgress(Guid studentId, Guid lessonId, Guid tenantId, bool completed, bool? examPassed)
    {
        var progress = (Progress)Activator.CreateInstance(typeof(Progress), true)!;
        progress.GetType().GetProperty("StudentId")!.SetValue(progress, studentId);
        progress.GetType().GetProperty("LessonId")!.SetValue(progress, lessonId);
        progress.GetType().GetProperty("TenantId")!.SetValue(progress, tenantId);
        if (completed)
        {
            progress.GetType().GetProperty("CompletedAt")!.SetValue(progress, DateTime.UtcNow);
        }
        progress.GetType().GetProperty("ExamPassed")!.SetValue(progress, examPassed);
        return progress;
    }
}
