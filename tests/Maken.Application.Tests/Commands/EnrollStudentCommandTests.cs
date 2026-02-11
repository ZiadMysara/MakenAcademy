using Maken.Application.Commands.Students;
using Maken.Application.Common.Interfaces;
using Maken.Domain.Entities;
using Moq;
using Microsoft.Extensions.Logging;

namespace Maken.Application.Tests.Commands;

/// <summary>
/// Unit tests for EnrollStudentCommandHandler.
/// Tests student enrollment with tenant isolation and validation.
/// </summary>
public class EnrollStudentCommandTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ITenantContext> _tenantContextMock;
    private readonly Mock<IRepository<Course>> _courseRepositoryMock;
    private readonly Mock<IRepository<User>> _userRepositoryMock;
    private readonly Mock<IRepository<Enrollment>> _enrollmentRepositoryMock;
    private readonly EnrollStudentCommandHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _courseId = Guid.NewGuid();
    private readonly Guid _studentId = Guid.NewGuid();

    public EnrollStudentCommandTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _tenantContextMock = new Mock<ITenantContext>();
        _courseRepositoryMock = new Mock<IRepository<Course>>();
        _userRepositoryMock = new Mock<IRepository<User>>();
        _enrollmentRepositoryMock = new Mock<IRepository<Enrollment>>();

        _tenantContextMock.Setup(x => x.TenantId).Returns(_tenantId);
        _unitOfWorkMock.Setup(x => x.Repository<Course>()).Returns(_courseRepositoryMock.Object);
        _unitOfWorkMock.Setup(x => x.Repository<User>()).Returns(_userRepositoryMock.Object);
        _unitOfWorkMock.Setup(x => x.Repository<Enrollment>()).Returns(_enrollmentRepositoryMock.Object);

        _handler = new EnrollStudentCommandHandler(_unitOfWorkMock.Object, _tenantContextMock.Object, new Mock<ILogger<EnrollStudentCommandHandler>>().Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_EnrollsStudent()
    {
        // Arrange
        var course = new Course(_tenantId, "Test Course", "Test Description");

        var student = new User("john.doe@example.com", "hashedpassword", "John", "Doe", Domain.Enums.RoleType.Student, _tenantId);

        _courseRepositoryMock
            .Setup(x => x.GetByIdAsync(_courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(course);

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(_studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(student);

        _enrollmentRepositoryMock
            .Setup(x => x.GetAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Enrollment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Enrollment>());

        var command = new EnrollStudentCommand(
            StudentId: _studentId,
            CourseId: _courseId
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(_studentId, result.StudentId);
        Assert.Equal(_courseId, result.CourseId);
        Assert.Equal(_tenantId, result.TenantId);

        _enrollmentRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Enrollment>(), It.IsAny<CancellationToken>()),
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
        _courseRepositoryMock
            .Setup(x => x.GetByIdAsync(_courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Course?)null);

        var command = new EnrollStudentCommand(
            StudentId: _studentId,
            CourseId: _courseId
        );

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _handler.Handle(command, CancellationToken.None)
        );

        _enrollmentRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Enrollment>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_WithNonExistentStudent_ThrowsKeyNotFoundException()
    {
        // Arrange
        var course = new Course(_tenantId, "Test Course", "Test Description");

        _courseRepositoryMock
            .Setup(x => x.GetByIdAsync(_courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(course);

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(_studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var command = new EnrollStudentCommand(
            StudentId: _studentId,
            CourseId: _courseId
        );

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _handler.Handle(command, CancellationToken.None)
        );

        _enrollmentRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Enrollment>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_WithCourseBelongingToDifferentTenant_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var differentTenantId = Guid.NewGuid();
        var course = new Course(differentTenantId, "Test Course", "Test Description");

        _courseRepositoryMock
            .Setup(x => x.GetByIdAsync(_courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(course);

        var command = new EnrollStudentCommand(
            StudentId: _studentId,
            CourseId: _courseId
        );

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _handler.Handle(command, CancellationToken.None)
        );

        _enrollmentRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Enrollment>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_WithStudentBelongingToDifferentTenant_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var course = new Course(_tenantId, "Test Course", "Test Description");

        var differentTenantId = Guid.NewGuid();
        var student = new User("john.doe@example.com", "hashedpassword", "John", "Doe", Domain.Enums.RoleType.Student, differentTenantId);

        _courseRepositoryMock
            .Setup(x => x.GetByIdAsync(_courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(course);

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(_studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(student);

        var command = new EnrollStudentCommand(
            StudentId: _studentId,
            CourseId: _courseId
        );

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _handler.Handle(command, CancellationToken.None)
        );

        _enrollmentRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Enrollment>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_WithExistingEnrollment_ThrowsInvalidOperationException()
    {
        // Arrange
        var course = new Course(_tenantId, "Test Course", "Test Description");

        var student = new User("john.doe@example.com", "hashedpassword", "John", "Doe", Domain.Enums.RoleType.Student, _tenantId);

        var existingEnrollment = new Enrollment
        {
            StudentId = _studentId,
            CourseId = _courseId,
            TenantId = _tenantId,
            EnrolledAt = DateTime.UtcNow
        };

        _courseRepositoryMock
            .Setup(x => x.GetByIdAsync(_courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(course);

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(_studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(student);

        _enrollmentRepositoryMock
            .Setup(x => x.GetAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Enrollment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Enrollment> { existingEnrollment });

        var command = new EnrollStudentCommand(
            StudentId: _studentId,
            CourseId: _courseId
        );

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, CancellationToken.None)
        );

        _enrollmentRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Enrollment>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_WithoutTenantContext_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _tenantContextMock.Setup(x => x.TenantId).Returns((Guid?)null);

        var command = new EnrollStudentCommand(
            StudentId: _studentId,
            CourseId: _courseId
        );

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _handler.Handle(command, CancellationToken.None)
        );

        _enrollmentRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Enrollment>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_WithValidCommand_SetsEnrolledAtTimestamp()
    {
        // Arrange
        var course = new Course(_tenantId, "Test Course", "Test Description");

        var student = new User("john.doe@example.com", "hashedpassword", "John", "Doe", Domain.Enums.RoleType.Student, _tenantId);

        _courseRepositoryMock
            .Setup(x => x.GetByIdAsync(_courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(course);

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(_studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(student);

        _enrollmentRepositoryMock
            .Setup(x => x.GetAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Enrollment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Enrollment>());

        var command = new EnrollStudentCommand(
            StudentId: _studentId,
            CourseId: _courseId
        );

        var beforeEnrollment = DateTime.UtcNow;

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        var afterEnrollment = DateTime.UtcNow;
        Assert.InRange(result.EnrolledAt, beforeEnrollment, afterEnrollment);
    }
}
