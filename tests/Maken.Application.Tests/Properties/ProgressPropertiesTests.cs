using FsCheck;
using FsCheck.Xunit;
using Maken.Application.Commands.Students;
using Maken.Application.Common.Interfaces;
using Maken.Domain.Entities;
using Maken.Domain.Enums;
using Moq;
using Microsoft.Extensions.Logging;

namespace Maken.Application.Tests.Properties;

/// <summary>
/// Property-based tests for progress tracking.
/// Tests Property 14 from design.md.
/// </summary>
public class ProgressPropertiesTests
{
    /// <summary>
    /// Property 14: Lesson Completion Progress Update
    /// **Validates: Requirements FR-025**
    /// 
    /// For any student and lesson where the student is enrolled in the lesson's course,
    /// completing the lesson should update the student's progress record with CompletedAt
    /// timestamp and potentially unlock the next lesson in sequence.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool LessonCompletionProgressUpdate_CreatesOrUpdatesProgressRecord(
        Guid studentId,
        Guid lessonId,
        Guid tenantId)
    {
        // Arrange
        var progressRepositoryMock = new Mock<IProgressRepository>();
        var tenantContextMock = new Mock<ITenantContext>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();

        tenantContextMock.Setup(x => x.TenantId).Returns(tenantId);

        // Simulate no existing progress
        progressRepositoryMock
            .Setup(x => x.GetByStudentAndLessonAsync(studentId, lessonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Progress?)null);

        Progress? capturedProgress = null;
        progressRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Progress>(), It.IsAny<CancellationToken>()))
            .Callback<Progress, CancellationToken>((p, ct) => capturedProgress = p)
            .Returns(Task.CompletedTask);

        var handler = new CompleteLessonCommandHandler(
            progressRepositoryMock.Object,
            tenantContextMock.Object,
            unitOfWorkMock.Object,
            new Mock<ILogger<CompleteLessonCommandHandler>>().Object
        );

        var command = new CompleteLessonCommand(studentId, lessonId);

        // Act
        var beforeCompletion = DateTime.UtcNow;
        handler.Handle(command, CancellationToken.None).Wait();
        var afterCompletion = DateTime.UtcNow;

        // Assert
        var progressCreated = capturedProgress != null;
        var hasCorrectStudentId = capturedProgress?.StudentId == studentId;
        var hasCorrectLessonId = capturedProgress?.LessonId == lessonId;
        var hasCorrectTenantId = capturedProgress?.TenantId == tenantId;
        var hasCompletedAt = capturedProgress?.CompletedAt != null;
        var completedAtInRange = capturedProgress?.CompletedAt >= beforeCompletion &&
                                capturedProgress?.CompletedAt <= afterCompletion;

        return progressCreated &&
                hasCorrectStudentId &&
                hasCorrectLessonId &&
                hasCorrectTenantId &&
                hasCompletedAt &&
                completedAtInRange;
    }

    [Property(MaxTest = 100)]
    public bool LessonCompletionProgressUpdate_UpdatesExistingProgressRecord(
        Guid studentId,
        Guid lessonId,
        Guid tenantId)
    {
        // Arrange
        var progressRepositoryMock = new Mock<IProgressRepository>();
        var tenantContextMock = new Mock<ITenantContext>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();

        tenantContextMock.Setup(x => x.TenantId).Returns(tenantId);

        // Simulate existing progress without completion
        var existingProgress = Progress.Create(studentId, lessonId, tenantId);
        progressRepositoryMock
            .Setup(x => x.GetByStudentAndLessonAsync(studentId, lessonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProgress);

        var handler = new CompleteLessonCommandHandler(
            progressRepositoryMock.Object,
            tenantContextMock.Object,
            unitOfWorkMock.Object,
            new Mock<ILogger<CompleteLessonCommandHandler>>().Object
        );

        var command = new CompleteLessonCommand(studentId, lessonId);

        // Act
        var beforeCompletion = DateTime.UtcNow;
        handler.Handle(command, CancellationToken.None).Wait();
        var afterCompletion = DateTime.UtcNow;

        // Assert
        var wasUpdated = existingProgress.CompletedAt != null;
        var completedAtInRange = existingProgress.CompletedAt >= beforeCompletion &&
                                existingProgress.CompletedAt <= afterCompletion;
        var updateWasCalled = true; // Mock was called

        progressRepositoryMock.Verify(
            x => x.Update(existingProgress),
            Times.Once
        );

        return wasUpdated && completedAtInRange && updateWasCalled;
    }

    [Property(MaxTest = 100)]
    public bool LessonCompletionProgressUpdate_SavesChangesToDatabase(
        Guid studentId,
        Guid lessonId,
        Guid tenantId)
    {
        // Arrange
        var progressRepositoryMock = new Mock<IProgressRepository>();
        var tenantContextMock = new Mock<ITenantContext>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();

        tenantContextMock.Setup(x => x.TenantId).Returns(tenantId);

        progressRepositoryMock
            .Setup(x => x.GetByStudentAndLessonAsync(studentId, lessonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Progress?)null);

        var handler = new CompleteLessonCommandHandler(
            progressRepositoryMock.Object,
            tenantContextMock.Object,
            unitOfWorkMock.Object,
            new Mock<ILogger<CompleteLessonCommandHandler>>().Object
        );

        var command = new CompleteLessonCommand(studentId, lessonId);

        // Act
        handler.Handle(command, CancellationToken.None).Wait();

        // Assert - SaveChangesAsync should be called exactly once
        unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once
        );

        return true;
    }
}
