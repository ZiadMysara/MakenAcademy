using Maken.Application.Common.Interfaces;
using Maken.Application.Queries.Lessons;
using Maken.Domain.Entities;
using Maken.Domain.Enums;
using Moq;

namespace Maken.Application.Tests.Queries;

/// <summary>
/// Unit tests for GetLessonsQueryHandler.
/// Tests retrieving lessons for a course with tenant isolation.
/// </summary>
public class GetLessonsQueryTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ITenantContext> _tenantContextMock;
    private readonly Mock<IRepository<Lesson>> _lessonRepositoryMock;
    private readonly Mock<IRepository<Course>> _courseRepositoryMock;
    private readonly GetLessonsQueryHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _courseId = Guid.NewGuid();

    public GetLessonsQueryTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _tenantContextMock = new Mock<ITenantContext>();
        _lessonRepositoryMock = new Mock<IRepository<Lesson>>();
        _courseRepositoryMock = new Mock<IRepository<Course>>();

        _tenantContextMock.Setup(x => x.TenantId).Returns(_tenantId);
        _unitOfWorkMock.Setup(x => x.Repository<Lesson>()).Returns(_lessonRepositoryMock.Object);
        _unitOfWorkMock.Setup(x => x.Repository<Course>()).Returns(_courseRepositoryMock.Object);

        _handler = new GetLessonsQueryHandler(_unitOfWorkMock.Object, _tenantContextMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCourseId_ReturnsLessonsOrderedByOrder()
    {
        // Arrange
        var course = Course.Create("Test Course", "Description", _tenantId);
        _courseRepositoryMock.Setup(x => x.GetByIdAsync(_courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(course);

        var lesson1 = Lesson.Create(_courseId, _tenantId, "Lesson 1", "Description 1", ContentType.Video, "http://example.com/video1", 1);
        var lesson2 = Lesson.Create(_courseId, _tenantId, "Lesson 2", "Description 2", ContentType.PDF, "http://example.com/pdf1", 2);
        var lesson3 = Lesson.Create(_courseId, _tenantId, "Lesson 3", "Description 3", ContentType.Video, "http://example.com/video3", 3);

        _lessonRepositoryMock.Setup(x => x.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Lesson, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Lesson> { lesson3, lesson1, lesson2 });

        var query = new GetLessonsQuery(_courseId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(3, result.Lessons.Count);
        Assert.Equal(1, result.Lessons[0].Order);
        Assert.Equal(2, result.Lessons[1].Order);
        Assert.Equal(3, result.Lessons[2].Order);
    }

    [Fact]
    public async Task Handle_WithNonExistentCourse_ThrowsKeyNotFoundException()
    {
        // Arrange
        _courseRepositoryMock.Setup(x => x.GetByIdAsync(_courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Course?)null);

        var query = new GetLessonsQuery(_courseId);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _handler.Handle(query, CancellationToken.None)
        );
    }

    [Fact]
    public async Task Handle_WithCourseFromDifferentTenant_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var differentTenantId = Guid.NewGuid();
        var course = Course.Create("Test Course", "Description", differentTenantId);
        _courseRepositoryMock.Setup(x => x.GetByIdAsync(_courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(course);

        var query = new GetLessonsQuery(_courseId);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _handler.Handle(query, CancellationToken.None)
        );
    }

    [Fact]
    public async Task Handle_WithoutTenantContext_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _tenantContextMock.Setup(x => x.TenantId).Returns((Guid?)null);
        var query = new GetLessonsQuery(_courseId);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _handler.Handle(query, CancellationToken.None)
        );
    }

    [Fact]
    public async Task Handle_WithCourseHavingNoLessons_ReturnsEmptyList()
    {
        // Arrange
        var course = Course.Create("Test Course", "Description", _tenantId);
        _courseRepositoryMock.Setup(x => x.GetByIdAsync(_courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(course);

        _lessonRepositoryMock.Setup(x => x.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Lesson, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Lesson>());

        var query = new GetLessonsQuery(_courseId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Empty(result.Lessons);
    }
}
