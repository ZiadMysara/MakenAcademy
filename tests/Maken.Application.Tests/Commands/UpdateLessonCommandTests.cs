using Maken.Application.Commands.Lessons;
using Maken.Application.Common.Interfaces;
using Maken.Domain.Entities;
using Maken.Domain.Enums;
using Moq;
using Microsoft.Extensions.Logging;

namespace Maken.Application.Tests.Commands;

/// <summary>
/// Unit tests for UpdateLessonCommandHandler.
/// Tests lesson updates with tenant isolation and validation.
/// </summary>
public class UpdateLessonCommandTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ITenantContext> _tenantContextMock;
    private readonly Mock<IRepository<Lesson>> _lessonRepositoryMock;
    private readonly UpdateLessonCommandHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _courseId = Guid.NewGuid();

    public UpdateLessonCommandTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _tenantContextMock = new Mock<ITenantContext>();
        _lessonRepositoryMock = new Mock<IRepository<Lesson>>();

        _tenantContextMock.Setup(x => x.TenantId).Returns(_tenantId);
        _unitOfWorkMock.Setup(x => x.Repository<Lesson>()).Returns(_lessonRepositoryMock.Object);

        _handler = new UpdateLessonCommandHandler(_unitOfWorkMock.Object, _tenantContextMock.Object, new Mock<ILogger<UpdateLessonCommandHandler>>().Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_UpdatesLesson()
    {
        // Arrange
        var lesson = Lesson.Create(_courseId, _tenantId, "Original Title", "Original Description", ContentType.Video, "http://example.com/video", 1);
        _lessonRepositoryMock.Setup(x => x.GetByIdAsync(lesson.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lesson);

        var command = new UpdateLessonCommand(
            Id: lesson.Id,
            Title: "Updated Title",
            Description: "Updated Description",
            ContentType: "PDF",
            ContentUrl: "http://example.com/document.pdf",
            Order: 2
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(lesson.Id, result.Id);
        Assert.Equal("Updated Title", result.Title);
        Assert.Equal("Updated Description", result.Description);
        Assert.Equal("PDF", result.ContentType);
        Assert.Equal("http://example.com/document.pdf", result.ContentUrl);
        Assert.Equal(2, result.Order);
        Assert.NotNull(result.UpdatedAt);

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

        var command = new UpdateLessonCommand(
            Id: lessonId,
            Title: "Updated Title",
            Description: "Updated Description",
            ContentType: "Video",
            ContentUrl: "http://example.com/video",
            Order: 1
        );

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
        var lesson = Lesson.Create(_courseId, differentTenantId, "Original Title", "Original Description", ContentType.Video, "http://example.com/video", 1);
        _lessonRepositoryMock.Setup(x => x.GetByIdAsync(lesson.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lesson);

        var command = new UpdateLessonCommand(
            Id: lesson.Id,
            Title: "Updated Title",
            Description: "Updated Description",
            ContentType: "Video",
            ContentUrl: "http://example.com/video",
            Order: 1
        );

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
        var command = new UpdateLessonCommand(
            Id: lessonId,
            Title: "Updated Title",
            Description: "Updated Description",
            ContentType: "Video",
            ContentUrl: "http://example.com/video",
            Order: 1
        );

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
    public async Task Handle_WithValidCommand_SetsUpdatedAtTimestamp()
    {
        // Arrange
        var lesson = Lesson.Create(_courseId, _tenantId, "Original Title", "Original Description", ContentType.Video, "http://example.com/video", 1);
        _lessonRepositoryMock.Setup(x => x.GetByIdAsync(lesson.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lesson);

        var command = new UpdateLessonCommand(
            Id: lesson.Id,
            Title: "Updated Title",
            Description: "Updated Description",
            ContentType: "Video",
            ContentUrl: "http://example.com/video",
            Order: 1
        );
        var beforeUpdate = DateTime.UtcNow;

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        var afterUpdate = DateTime.UtcNow;
        Assert.NotNull(result.UpdatedAt);
        Assert.InRange(result.UpdatedAt, beforeUpdate, afterUpdate);
    }
}
