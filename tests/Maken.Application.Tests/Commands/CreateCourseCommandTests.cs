using Maken.Application.Commands.Courses;
using Maken.Application.Common.Interfaces;
using Maken.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;

namespace Maken.Application.Tests.Commands;

/// <summary>
/// Unit tests for CreateCourseCommandHandler.
/// Tests course creation with tenant isolation and validation.
/// </summary>
public class CreateCourseCommandTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ITenantContext> _tenantContextMock;
    private readonly Mock<IRepository<Course>> _courseRepositoryMock;
    private readonly Mock<ILogger<CreateCourseCommandHandler>> _loggerMock;
    private readonly CreateCourseCommandHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    public CreateCourseCommandTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _tenantContextMock = new Mock<ITenantContext>();
        _courseRepositoryMock = new Mock<IRepository<Course>>();
        _loggerMock = new Mock<ILogger<CreateCourseCommandHandler>>();

        _tenantContextMock.Setup(x => x.TenantId).Returns(_tenantId);
        _unitOfWorkMock.Setup(x => x.Repository<Course>()).Returns(_courseRepositoryMock.Object);

        _handler = new CreateCourseCommandHandler(_unitOfWorkMock.Object, _tenantContextMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_CreatesCourse()
    {
        // Arrange
        var command = new CreateCourseCommand(
            Name: "Test Course",
            Description: "Test Description",
            FreeFlowMode: false,
            PrerequisiteCourseIds: new List<Guid>()
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(_tenantId, result.TenantId);
        Assert.Equal("Test Course", result.Name);
        Assert.Equal("Test Description", result.Description);
        Assert.Equal("Draft", result.Status);
        Assert.False(result.FreeFlowMode);
        Assert.Empty(result.PrerequisiteCourseIds);

        _courseRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Course>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_WithFreeFlowMode_CreatesCourseWithFreeFlowEnabled()
    {
        // Arrange
        var command = new CreateCourseCommand(
            Name: "Free Flow Course",
            Description: "Description",
            FreeFlowMode: true,
            PrerequisiteCourseIds: new List<Guid>()
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.FreeFlowMode);
    }

    [Fact]
    public async Task Handle_WithPrerequisites_CreatesCourseWithPrerequisites()
    {
        // Arrange
        var prerequisite1 = Guid.NewGuid();
        var prerequisite2 = Guid.NewGuid();
        var command = new CreateCourseCommand(
            Name: "Advanced Course",
            Description: "Description",
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
    public async Task Handle_WithoutTenantContext_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _tenantContextMock.Setup(x => x.TenantId).Returns((Guid?)null);
        var command = new CreateCourseCommand(
            Name: "Test Course",
            Description: "Description",
            FreeFlowMode: false,
            PrerequisiteCourseIds: new List<Guid>()
        );

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _handler.Handle(command, CancellationToken.None)
        );

        _courseRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Course>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_WithValidCommand_SetsCreatedAtTimestamp()
    {
        // Arrange
        var command = new CreateCourseCommand(
            Name: "Test Course",
            Description: "Description",
            FreeFlowMode: false,
            PrerequisiteCourseIds: new List<Guid>()
        );
        var beforeCreation = DateTime.UtcNow;

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        var afterCreation = DateTime.UtcNow;
        Assert.InRange(result.CreatedAt, beforeCreation, afterCreation);
    }

    [Fact]
    public async Task Handle_WithValidCommand_SetsDraftStatus()
    {
        // Arrange
        var command = new CreateCourseCommand(
            Name: "Test Course",
            Description: "Description",
            FreeFlowMode: false,
            PrerequisiteCourseIds: new List<Guid>()
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal("Draft", result.Status);
    }
}
