using Maken.Application.Commands.Lessons;
using Maken.Application.Common.Interfaces;
using Maken.Domain.Entities;
using Maken.Domain.Enums;
using Moq;
using Microsoft.Extensions.Logging;

namespace Maken.Application.Tests.Commands;

/// <summary>
/// Unit tests for DeleteLessonCommandHandler.
/// Tests lesson soft deletion with tenant isolation and cascade behavior.
/// </summary>
public class DeleteLessonCommandTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ITenantContext> _tenantContextMock;
    private readonly Mock<IRepository<Lesson>> _lessonRepositoryMock;
    private readonly DeleteLessonCommandHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _courseId = Guid.NewGuid();

    public DeleteLessonCommandTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _tenantContextMock = new Mock<ITenantContext>();
        _lessonRepositoryMock = new Mock<IRepository<Lesson>>();

        _tenantContextMock.Setup(x => x.TenantId).Returns(_tenantId);
        _unitOfWorkMock.Setup(x => x.Repository<Lesson>()).Returns(_lessonRepositoryMock.Object);

        _handler = new DeleteLessonCommandHandler(_unitOfWorkMock.Object, _tenantContextMock.Object, new Mock<ILogger<DeleteLessonCommandHandler>>().Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_SoftDeletesLesson()
    {
        // Arrange
        var lesson = Lesson.Create(_courseId, _tenantId, "Test Lesson", "Test Description", ContentType.Video, "http://example.com/video", 1);
        _lessonRepositoryMock.Setup(x => x.GetByIdAsync(lesson.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lesson);

        var command = new DeleteLessonCommand(lesson.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(lesson.Id, result.Id);
        Assert.NotNull(result.DeletedAt);
        Assert.True(lesson.IsDeleted);

        _lessonRepositoryMock.Verify(
            x => x.Update(It.IsAny<Lesson>()),
            Times.Once
        );
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_WithNonExistentLesson_ThrowsKeyNotFoundException()
    {
        // Arrange
        var lessonId = Guid.NewGuid();
        _lessonRepositoryMock.Setup(x => x.GetByIdAsync(lessonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Lesson?)null);

        var command = new DeleteLessonCommand(lessonId);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _handler.Handle(command, CancellationToken.None)
        );

        _lessonRepositoryMock.Verify(
            x => x.Update(It.IsAny<Lesson>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_WithLessonFromDifferentTenant_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var differentTenantId = Guid.NewGuid();
        var lesson = Lesson.Create(_courseId, differentTenantId, "Test Lesson", "Test Description", ContentType.Video, "http://example.com/video", 1);
        _lessonRepositoryMock.Setup(x => x.GetByIdAsync(lesson.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lesson);

        var command = new DeleteLessonCommand(lesson.Id);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _handler.Handle(command, CancellationToken.None)
        );

        _lessonRepositoryMock.Verify(
            x => x.Update(It.IsAny<Lesson>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_WithoutTenantContext_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _tenantContextMock.Setup(x => x.TenantId).Returns((Guid?)null);
        var lessonId = Guid.NewGuid();
        var command = new DeleteLessonCommand(lessonId);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _handler.Handle(command, CancellationToken.None)
        );

        _lessonRepositoryMock.Verify(
            x => x.Update(It.IsAny<Lesson>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_WithValidCommand_SetsDeletedAtTimestamp()
    {
        // Arrange
        var lesson = Lesson.Create(_courseId, _tenantId, "Test Lesson", "Test Description", ContentType.Video, "http://example.com/video", 1);
        _lessonRepositoryMock.Setup(x => x.GetByIdAsync(lesson.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lesson);

        var command = new DeleteLessonCommand(lesson.Id);
        var beforeDeletion = DateTime.UtcNow;

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        var afterDeletion = DateTime.UtcNow;
        Assert.InRange(result.DeletedAt, beforeDeletion, afterDeletion);
    }
}
