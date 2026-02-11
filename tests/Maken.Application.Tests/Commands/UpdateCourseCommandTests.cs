using Maken.Application.Commands.Courses;
using Maken.Application.Common.Interfaces;
using Maken.Application.Tests.Helpers;
using Maken.Domain.Entities;
using Maken.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;

namespace Maken.Application.Tests.Commands;

/// <summary>
/// Unit tests for UpdateCourseCommandHandler.
/// Tests course updates with tenant isolation and validation.
/// </summary>
public class UpdateCourseCommandTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ITenantContext> _tenantContextMock;
    private readonly Mock<IRepository<Course>> _courseRepositoryMock;
    private readonly Mock<ILogger<UpdateCourseCommandHandler>> _loggerMock;
    private readonly UpdateCourseCommandHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    public UpdateCourseCommandTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _tenantContextMock = new Mock<ITenantContext>();
        _courseRepositoryMock = new Mock<IRepository<Course>>();
        _loggerMock = new Mock<ILogger<UpdateCourseCommandHandler>>();

        _tenantContextMock.Setup(x => x.TenantId).Returns(_tenantId);
        _unitOfWorkMock.Setup(x => x.Repository<Course>()).Returns(_courseRepositoryMock.Object);

        _handler = new UpdateCourseCommandHandler(_unitOfWorkMock.Object, _tenantContextMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_UpdatesCourse()
    {
        // Arrange
        var courseId = Guid.NewGuid();
        var existingCourse = Course.Create("Original Name", "Original Description", _tenantId);
        EntityTestHelper.SetId(existingCourse, courseId);

        _courseRepositoryMock
            .Setup(x => x.GetByIdAsync(courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCourse);

        var command = new UpdateCourseCommand(
            Id: courseId,
            Name: "Updated Name",
            Description: "Updated Description",
            FreeFlowMode: true,
            PrerequisiteCourseIds: new List<Guid>()
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(courseId, result.Id);
        Assert.Equal("Updated Name", result.Name);
        Assert.Equal("Updated Description", result.Description);
        Assert.True(result.FreeFlowMode);

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

        var command = new UpdateCourseCommand(
            Id: courseId,
            Name: "Updated Name",
            Description: "Updated Description",
            FreeFlowMode: false,
            PrerequisiteCourseIds: new List<Guid>()
        );

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
        var existingCourse = Course.Create("Original Name", "Original Description", differentTenantId);
        EntityTestHelper.SetId(existingCourse, courseId);

        _courseRepositoryMock
            .Setup(x => x.GetByIdAsync(courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCourse);

        var command = new UpdateCourseCommand(
            Id: courseId,
            Name: "Updated Name",
            Description: "Updated Description",
            FreeFlowMode: false,
            PrerequisiteCourseIds: new List<Guid>()
        );

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
        var command = new UpdateCourseCommand(
            Id: Guid.NewGuid(),
            Name: "Updated Name",
            Description: "Updated Description",
            FreeFlowMode: false,
            PrerequisiteCourseIds: new List<Guid>()
        );

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _handler.Handle(command, CancellationToken.None)
        );
    }

    [Fact]
    public async Task Handle_WithPrerequisites_UpdatesPrerequisites()
    {
        // Arrange
        var courseId = Guid.NewGuid();
        var prerequisite1 = Guid.NewGuid();
        var prerequisite2 = Guid.NewGuid();
        var existingCourse = Course.Create("Original Name", "Original Description", _tenantId);
        EntityTestHelper.SetId(existingCourse, courseId);

        _courseRepositoryMock
            .Setup(x => x.GetByIdAsync(courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCourse);

        var command = new UpdateCourseCommand(
            Id: courseId,
            Name: "Updated Name",
            Description: "Updated Description",
            FreeFlowMode: false,
            PrerequisiteCourseIds: new List<Guid> { prerequisite1, prerequisite2 }
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.PrerequisiteCourseIds.Count);
        Assert.Contains(prerequisite1, result.PrerequisiteCourseIds);
        Assert.Contains(prerequisite2, result.PrerequisiteCourseIds);
    }

    [Fact]
    public async Task Handle_WithValidCommand_PreservesOriginalId()
    {
        // Arrange
        var courseId = Guid.NewGuid();
        var existingCourse = Course.Create("Original Name", "Original Description", _tenantId);
        EntityTestHelper.SetId(existingCourse, courseId);

        _courseRepositoryMock
            .Setup(x => x.GetByIdAsync(courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCourse);

        var command = new UpdateCourseCommand(
            Id: courseId,
            Name: "Updated Name",
            Description: "Updated Description",
            FreeFlowMode: false,
            PrerequisiteCourseIds: new List<Guid>()
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(courseId, result.Id);
    }

    [Fact]
    public async Task Handle_WithValidCommand_SetsUpdatedAtTimestamp()
    {
        // Arrange
        var courseId = Guid.NewGuid();
        var existingCourse = Course.Create("Original Name", "Original Description", _tenantId);
        EntityTestHelper.SetId(existingCourse, courseId);

        _courseRepositoryMock
            .Setup(x => x.GetByIdAsync(courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCourse);

        var command = new UpdateCourseCommand(
            Id: courseId,
            Name: "Updated Name",
            Description: "Updated Description",
            FreeFlowMode: false,
            PrerequisiteCourseIds: new List<Guid>()
        );

        var beforeUpdate = DateTime.UtcNow;

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        var afterUpdate = DateTime.UtcNow;
        Assert.InRange(result.UpdatedAt, beforeUpdate, afterUpdate);
    }
}
