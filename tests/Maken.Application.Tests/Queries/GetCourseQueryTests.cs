using Maken.Application.Common.Interfaces;
using Maken.Application.Queries.Courses;
using Maken.Application.Tests.Helpers;
using Maken.Domain.Entities;
using Moq;

namespace Maken.Application.Tests.Queries;

/// <summary>
/// Unit tests for GetCourseQueryHandler.
/// Tests course retrieval with tenant isolation.
/// </summary>
public class GetCourseQueryTests
{
    private readonly Mock<IRepository<Course>> _courseRepositoryMock;
    private readonly Mock<ITenantContext> _tenantContextMock;
    private readonly GetCourseQueryHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    public GetCourseQueryTests()
    {
        _courseRepositoryMock = new Mock<IRepository<Course>>();
        _tenantContextMock = new Mock<ITenantContext>();

        _tenantContextMock.Setup(x => x.TenantId).Returns(_tenantId);

        _handler = new GetCourseQueryHandler(_courseRepositoryMock.Object, _tenantContextMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidId_ReturnsCourse()
    {
        // Arrange
        var courseId = Guid.NewGuid();
        var course = Course.Create("Test Course", "Test Description", _tenantId);
        EntityTestHelper.SetId(course, courseId);

        _courseRepositoryMock
            .Setup(x => x.GetByIdAsync(courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(course);

        var query = new GetCourseQuery(courseId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(courseId, result.Id);
        Assert.Equal("Test Course", result.Name);
        Assert.Equal("Test Description", result.Description);
        Assert.Equal(_tenantId, result.TenantId);
    }

    [Fact]
    public async Task Handle_WithNonExistentId_ReturnsNull()
    {
        // Arrange
        var courseId = Guid.NewGuid();
        _courseRepositoryMock
            .Setup(x => x.GetByIdAsync(courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Course?)null);

        var query = new GetCourseQuery(courseId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_WithDifferentTenant_ReturnsNull()
    {
        // Arrange
        var courseId = Guid.NewGuid();
        var differentTenantId = Guid.NewGuid();
        var course = Course.Create("Test Course", "Test Description", differentTenantId);
        EntityTestHelper.SetId(course, courseId);

        _courseRepositoryMock
            .Setup(x => x.GetByIdAsync(courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(course);

        var query = new GetCourseQuery(courseId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Null(result); // Should return null for cross-tenant access
    }

    [Fact]
    public async Task Handle_WithoutTenantContext_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _tenantContextMock.Setup(x => x.TenantId).Returns((Guid?)null);
        var query = new GetCourseQuery(Guid.NewGuid());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _handler.Handle(query, CancellationToken.None)
        );
    }

    [Fact]
    public async Task Handle_WithValidId_ReturnsCourseWithCorrectStatus()
    {
        // Arrange
        var courseId = Guid.NewGuid();
        var course = Course.Create("Test Course", "Test Description", _tenantId);
        EntityTestHelper.SetId(course, courseId);
        course.Publish();

        _courseRepositoryMock
            .Setup(x => x.GetByIdAsync(courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(course);

        var query = new GetCourseQuery(courseId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Published", result.Status);
    }

    [Fact]
    public async Task Handle_WithValidId_ReturnsCourseWithPrerequisites()
    {
        // Arrange
        var courseId = Guid.NewGuid();
        var prerequisite1 = Guid.NewGuid();
        var prerequisite2 = Guid.NewGuid();
        var course = Course.Create("Test Course", "Test Description", _tenantId);
        EntityTestHelper.SetId(course, courseId);
        course.AddPrerequisite(prerequisite1);
        course.AddPrerequisite(prerequisite2);

        _courseRepositoryMock
            .Setup(x => x.GetByIdAsync(courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(course);

        var query = new GetCourseQuery(courseId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.PrerequisiteCourseIds.Count);
        Assert.Contains(prerequisite1, result.PrerequisiteCourseIds);
        Assert.Contains(prerequisite2, result.PrerequisiteCourseIds);
    }

    [Fact]
    public async Task Handle_WithValidId_ReturnsCourseWithFreeFlowMode()
    {
        // Arrange
        var courseId = Guid.NewGuid();
        var course = Course.Create("Test Course", "Test Description", _tenantId);
        EntityTestHelper.SetId(course, courseId);
        course.SetFreeFlowMode(true);

        _courseRepositoryMock
            .Setup(x => x.GetByIdAsync(courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(course);

        var query = new GetCourseQuery(courseId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.FreeFlowMode);
    }
}
