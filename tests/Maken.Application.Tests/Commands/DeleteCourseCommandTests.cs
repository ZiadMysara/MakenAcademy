using Maken.Application.Commands.Courses;
using Maken.Application.Common.Interfaces;
using Maken.Application.Tests.Helpers;
using Maken.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;

namespace Maken.Application.Tests.Commands;

/// <summary>
/// Unit tests for DeleteCourseCommandHandler.
/// Tests course soft deletion with tenant isolation and cascade behavior.
/// </summary>
public class DeleteCourseCommandTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ITenantContext> _tenantContextMock;
    private readonly Mock<IRepository<Course>> _courseRepositoryMock;
    private readonly Mock<ILogger<DeleteCourseCommandHandler>> _loggerMock;
    private readonly DeleteCourseCommandHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    public DeleteCourseCommandTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _tenantContextMock = new Mock<ITenantContext>();
        _courseRepositoryMock = new Mock<IRepository<Course>>();
        _loggerMock = new Mock<ILogger<DeleteCourseCommandHandler>>();

        _tenantContextMock.Setup(x => x.TenantId).Returns(_tenantId);
        _unitOfWorkMock.Setup(x => x.Repository<Course>()).Returns(_courseRepositoryMock.Object);

        _handler = new DeleteCourseCommandHandler(_unitOfWorkMock.Object, _tenantContextMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_SoftDeletesCourse()
    {
        // Arrange
        var courseId = Guid.NewGuid();
        var existingCourse = Course.Create("Test Course", "Test Description", _tenantId);
        EntityTestHelper.SetId(existingCourse, courseId);

        _courseRepositoryMock
            .Setup(x => x.GetByIdAsync(courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCourse);

        var command = new DeleteCourseCommand(courseId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(courseId, result.Id);
        Assert.True(existingCourse.IsDeleted);
        Assert.NotNull(existingCourse.DeletedAt);

        _courseRepositoryMock.Verify(
            x => x.Update(It.IsAny<Course>()),
            Times.Once
        );
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_WithNonExistentCourse_ThrowsKeyNotFoundException()
    {
        // Arrange
        var courseId = Guid.NewGuid();
        _courseRepositoryMock
            .Setup(x => x.GetByIdAsync(courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Course?)null);

        var command = new DeleteCourseCommand(courseId);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _handler.Handle(command, CancellationToken.None)
        );

        _courseRepositoryMock.Verify(
            x => x.Update(It.IsAny<Course>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_WithDifferentTenant_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var courseId = Guid.NewGuid();
        var differentTenantId = Guid.NewGuid();
        var existingCourse = Course.Create("Test Course", "Test Description", differentTenantId);
        EntityTestHelper.SetId(existingCourse, courseId);

        _courseRepositoryMock
            .Setup(x => x.GetByIdAsync(courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCourse);

        var command = new DeleteCourseCommand(courseId);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _handler.Handle(command, CancellationToken.None)
        );

        _courseRepositoryMock.Verify(
            x => x.Update(It.IsAny<Course>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_WithoutTenantContext_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _tenantContextMock.Setup(x => x.TenantId).Returns((Guid?)null);
        var command = new DeleteCourseCommand(Guid.NewGuid());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _handler.Handle(command, CancellationToken.None)
        );
    }

    [Fact]
    public async Task Handle_WithValidCommand_SetsDeletedAtTimestamp()
    {
        // Arrange
        var courseId = Guid.NewGuid();
        var existingCourse = Course.Create("Test Course", "Test Description", _tenantId);
        EntityTestHelper.SetId(existingCourse, courseId);

        _courseRepositoryMock
            .Setup(x => x.GetByIdAsync(courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCourse);

        var command = new DeleteCourseCommand(courseId);
        var beforeDeletion = DateTime.UtcNow;

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        var afterDeletion = DateTime.UtcNow;
        Assert.InRange(result.DeletedAt, beforeDeletion, afterDeletion);
        Assert.NotNull(existingCourse.DeletedAt);
    }

    [Fact]
    public async Task Handle_WithValidCommand_PreservesOriginalId()
    {
        // Arrange
        var courseId = Guid.NewGuid();
        var existingCourse = Course.Create("Test Course", "Test Description", _tenantId);
        EntityTestHelper.SetId(existingCourse, courseId);

        _courseRepositoryMock
            .Setup(x => x.GetByIdAsync(courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCourse);

        var command = new DeleteCourseCommand(courseId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(courseId, result.Id);
    }

    [Fact]
    public async Task Handle_WithAlreadyDeletedCourse_ThrowsKeyNotFoundException()
    {
        // Arrange
        var courseId = Guid.NewGuid();
        var existingCourse = Course.Create("Test Course", "Test Description", _tenantId);
        EntityTestHelper.SetId(existingCourse, courseId);
        existingCourse.SoftDelete();

        // Simulate global query filter excluding soft-deleted entities
        _courseRepositoryMock
            .Setup(x => x.GetByIdAsync(courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Course?)null);

        var command = new DeleteCourseCommand(courseId);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _handler.Handle(command, CancellationToken.None)
        );
    }
}
