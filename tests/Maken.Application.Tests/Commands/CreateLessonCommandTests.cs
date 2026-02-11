using Maken.Application.Commands.Lessons;
using Maken.Application.Common.Interfaces;
using Maken.Domain.Entities;
using Maken.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;

namespace Maken.Application.Tests.Commands;

/// <summary>
/// Unit tests for CreateLessonCommandHandler.
/// Tests lesson creation with tenant isolation and validation.
/// </summary>
public class CreateLessonCommandTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ITenantContext> _tenantContextMock;
    private readonly Mock<IRepository<Lesson>> _lessonRepositoryMock;
    private readonly Mock<IRepository<Course>> _courseRepositoryMock;
    private readonly Mock<ILogger<CreateLessonCommandHandler>> _loggerMock;
    private readonly CreateLessonCommandHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _courseId = Guid.NewGuid();

    public CreateLessonCommandTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _tenantContextMock = new Mock<ITenantContext>();
        _lessonRepositoryMock = new Mock<IRepository<Lesson>>();
        _courseRepositoryMock = new Mock<IRepository<Course>>();
        _loggerMock = new Mock<ILogger<CreateLessonCommandHandler>>();

        _tenantContextMock.Setup(x => x.TenantId).Returns(_tenantId);
        _unitOfWorkMock.Setup(x => x.Repository<Lesson>()).Returns(_lessonRepositoryMock.Object);
        _unitOfWorkMock.Setup(x => x.Repository<Course>()).Returns(_courseRepositoryMock.Object);

        _handler = new CreateLessonCommandHandler(_unitOfWorkMock.Object, _tenantContextMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_CreatesLesson()
    {
        // Arrange
        var course = Course.Create("Test Course", "Description", _tenantId);
        _courseRepositoryMock.Setup(x => x.GetByIdAsync(_courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(course);

        var command = new CreateLessonCommand(
            CourseId: _courseId,
            Title: "Test Lesson",
            Description: "Test Description",
            ContentType: "Video",
            ContentUrl: "http://example.com/video",
            Order: 1
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(_tenantId, result.TenantId);
        Assert.Equal("Test Lesson", result.Title);
        Assert.Equal("Test Description", result.Description);
        Assert.Equal("Video", result.ContentType);
        Assert.Equal("http://example.com/video", result.ContentUrl);
        Assert.Equal(1, result.Order);

        _lessonRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Lesson>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_WithPDFContentType_CreatesLessonWithPDFType()
    {
        // Arrange
        var course = Course.Create("Test Course", "Description", _tenantId);
        _courseRepositoryMock.Setup(x => x.GetByIdAsync(_courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(course);

        var command = new CreateLessonCommand(
            CourseId: _courseId,
            Title: "PDF Lesson",
            Description: "Description",
            ContentType: "PDF",
            ContentUrl: "http://example.com/document.pdf",
            Order: 1
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal("PDF", result.ContentType);
    }

    [Fact]
    public async Task Handle_WithNonExistentCourse_ThrowsKeyNotFoundException()
    {
        // Arrange
        _courseRepositoryMock.Setup(x => x.GetByIdAsync(_courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Course?)null);

        var command = new CreateLessonCommand(
            CourseId: _courseId,
            Title: "Test Lesson",
            Description: "Description",
            ContentType: "Video",
            ContentUrl: "http://example.com/video",
            Order: 1
        );

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _handler.Handle(command, CancellationToken.None)
        );

        _lessonRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Lesson>(), It.IsAny<CancellationToken>()),
            Times.Never
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

        var command = new CreateLessonCommand(
            CourseId: _courseId,
            Title: "Test Lesson",
            Description: "Description",
            ContentType: "Video",
            ContentUrl: "http://example.com/video",
            Order: 1
        );

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _handler.Handle(command, CancellationToken.None)
        );

        _lessonRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Lesson>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_WithoutTenantContext_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _tenantContextMock.Setup(x => x.TenantId).Returns((Guid?)null);
        var command = new CreateLessonCommand(
            CourseId: _courseId,
            Title: "Test Lesson",
            Description: "Description",
            ContentType: "Video",
            ContentUrl: "http://example.com/video",
            Order: 1
        );

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _handler.Handle(command, CancellationToken.None)
        );

        _lessonRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Lesson>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_WithValidCommand_SetsCreatedAtTimestamp()
    {
        // Arrange
        var course = Course.Create("Test Course", "Description", _tenantId);
        _courseRepositoryMock.Setup(x => x.GetByIdAsync(_courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(course);

        var command = new CreateLessonCommand(
            CourseId: _courseId,
            Title: "Test Lesson",
            Description: "Description",
            ContentType: "Video",
            ContentUrl: "http://example.com/video",
            Order: 1
        );
        var beforeCreation = DateTime.UtcNow;

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        var afterCreation = DateTime.UtcNow;
        Assert.InRange(result.CreatedAt, beforeCreation, afterCreation);
    }
}
