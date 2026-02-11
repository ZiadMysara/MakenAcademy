using Maken.Application.Commands.Students;
using Maken.Application.Common.Interfaces;
using Maken.Domain.Entities;
using MediatR;
using Moq;
using Microsoft.Extensions.Logging;

namespace Maken.Application.Tests.Commands;

/// <summary>
/// Unit tests for CompleteLessonCommandHandler.
/// Tests lesson completion with progress tracking and tenant isolation.
/// </summary>
public class CompleteLessonCommandTests
{
    private readonly Mock<IProgressRepository> _progressRepositoryMock;
    private readonly Mock<ITenantContext> _tenantContextMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly CompleteLessonCommandHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _studentId = Guid.NewGuid();
    private readonly Guid _lessonId = Guid.NewGuid();

    public CompleteLessonCommandTests()
    {
        _progressRepositoryMock = new Mock<IProgressRepository>();
        _tenantContextMock = new Mock<ITenantContext>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _tenantContextMock.Setup(x => x.TenantId).Returns(_tenantId);

        _handler = new CompleteLessonCommandHandler(
            _progressRepositoryMock.Object,
            _tenantContextMock.Object,
            _unitOfWorkMock.Object,
            new Mock<ILogger<CompleteLessonCommandHandler>>().Object
        );
    }

    [Fact]
    public async Task Handle_WithNoExistingProgress_CreatesNewProgress()
    {
        // Arrange
        var command = new CompleteLessonCommand(_studentId, _lessonId);
        _progressRepositoryMock
            .Setup(x => x.GetByStudentAndLessonAsync(_studentId, _lessonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Progress?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(Unit.Value, result);
        _progressRepositoryMock.Verify(
            x => x.AddAsync(It.Is<Progress>(p =>
                p.StudentId == _studentId &&
                p.LessonId == _lessonId &&
                p.TenantId == _tenantId &&
                p.CompletedAt != null
            ), It.IsAny<CancellationToken>()),
            Times.Once
        );
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_WithExistingProgress_UpdatesProgress()
    {
        // Arrange
        var command = new CompleteLessonCommand(_studentId, _lessonId);
        var existingProgress = Progress.Create(_studentId, _lessonId, _tenantId);
        
        _progressRepositoryMock
            .Setup(x => x.GetByStudentAndLessonAsync(_studentId, _lessonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProgress);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(Unit.Value, result);
        Assert.NotNull(existingProgress.CompletedAt);
        _progressRepositoryMock.Verify(
            x => x.Update(existingProgress),
            Times.Once
        );
        _progressRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Progress>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_WithoutTenantContext_ThrowsInvalidOperationException()
    {
        // Arrange
        _tenantContextMock.Setup(x => x.TenantId).Returns((Guid?)null);
        var command = new CompleteLessonCommand(_studentId, _lessonId);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, CancellationToken.None)
        );

        _progressRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Progress>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
        _progressRepositoryMock.Verify(
            x => x.Update(It.IsAny<Progress>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_WithNewProgress_SetsCompletedAtTimestamp()
    {
        // Arrange
        var command = new CompleteLessonCommand(_studentId, _lessonId);
        Progress? capturedProgress = null;
        
        _progressRepositoryMock
            .Setup(x => x.GetByStudentAndLessonAsync(_studentId, _lessonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Progress?)null);
        
        _progressRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Progress>(), It.IsAny<CancellationToken>()))
            .Callback<Progress, CancellationToken>((p, ct) => capturedProgress = p)
            .Returns(Task.CompletedTask);

        var beforeCompletion = DateTime.UtcNow;

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        var afterCompletion = DateTime.UtcNow;
        Assert.NotNull(capturedProgress);
        Assert.NotNull(capturedProgress.CompletedAt);
        Assert.InRange(capturedProgress.CompletedAt.Value, beforeCompletion, afterCompletion);
    }

    [Fact]
    public async Task Handle_WithExistingProgress_UpdatesCompletedAtTimestamp()
    {
        // Arrange
        var command = new CompleteLessonCommand(_studentId, _lessonId);
        var existingProgress = Progress.Create(_studentId, _lessonId, _tenantId);
        
        _progressRepositoryMock
            .Setup(x => x.GetByStudentAndLessonAsync(_studentId, _lessonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProgress);

        var beforeCompletion = DateTime.UtcNow;

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        var afterCompletion = DateTime.UtcNow;
        Assert.NotNull(existingProgress.CompletedAt);
        Assert.InRange(existingProgress.CompletedAt.Value, beforeCompletion, afterCompletion);
    }

    [Fact]
    public async Task Handle_WithValidCommand_CallsSaveChanges()
    {
        // Arrange
        var command = new CompleteLessonCommand(_studentId, _lessonId);
        _progressRepositoryMock
            .Setup(x => x.GetByStudentAndLessonAsync(_studentId, _lessonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Progress?)null);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once
        );
    }
}
