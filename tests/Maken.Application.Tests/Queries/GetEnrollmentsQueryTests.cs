using Maken.Application.Common.Interfaces;
using Maken.Application.Queries.Enrollments;
using Maken.Domain.Entities;
using Moq;

namespace Maken.Application.Tests.Queries;

/// <summary>
/// Unit tests for GetEnrollmentsQueryHandler.
/// Tests enrollment retrieval with tenant isolation and student information.
/// </summary>
public class GetEnrollmentsQueryTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ITenantContext> _tenantContextMock;
    private readonly Mock<IRepository<Course>> _courseRepositoryMock;
    private readonly Mock<IRepository<User>> _userRepositoryMock;
    private readonly Mock<IRepository<Enrollment>> _enrollmentRepositoryMock;
    private readonly GetEnrollmentsQueryHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _courseId = Guid.NewGuid();

    public GetEnrollmentsQueryTests()
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

        _handler = new GetEnrollmentsQueryHandler(_unitOfWorkMock.Object, _tenantContextMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCourseId_ReturnsEnrollments()
    {
        // Arrange
        var course = new Course(_tenantId, "Test Course", "Test Description", false, null);

        var studentId1 = Guid.NewGuid();
        var studentId2 = Guid.NewGuid();

        var student1 = new User("john.doe@example.com", "hashedpassword", "John", "Doe", Domain.Enums.RoleType.Student, _tenantId);
        var student2 = new User("jane.smith@example.com", "hashedpassword", "Jane", "Smith", Domain.Enums.RoleType.Student, _tenantId);

        var enrollment1 = new Enrollment
        {
            StudentId = studentId1,
            CourseId = _courseId,
            TenantId = _tenantId,
            EnrolledAt = DateTime.UtcNow.AddDays(-2)
        };

        var enrollment2 = new Enrollment
        {
            StudentId = studentId2,
            CourseId = _courseId,
            TenantId = _tenantId,
            EnrolledAt = DateTime.UtcNow.AddDays(-1)
        };

        _courseRepositoryMock
            .Setup(x => x.GetByIdAsync(_courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(course);

        _enrollmentRepositoryMock
            .Setup(x => x.GetAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Enrollment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Enrollment> { enrollment1, enrollment2 });

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(studentId1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(student1);

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(studentId2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(student2);

        var query = new GetEnrollmentsQuery(_courseId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        
        var firstEnrollment = result[0];
        Assert.Equal(studentId1, firstEnrollment.StudentId);
        Assert.Equal("John", firstEnrollment.StudentFirstName);
        Assert.Equal("Doe", firstEnrollment.StudentLastName);
        Assert.Equal("john.doe@example.com", firstEnrollment.StudentEmail);

        var secondEnrollment = result[1];
        Assert.Equal(studentId2, secondEnrollment.StudentId);
        Assert.Equal("Jane", secondEnrollment.StudentFirstName);
        Assert.Equal("Smith", secondEnrollment.StudentLastName);
        Assert.Equal("jane.smith@example.com", secondEnrollment.StudentEmail);
    }

    [Fact]
    public async Task Handle_WithNonExistentCourse_ThrowsKeyNotFoundException()
    {
        // Arrange
        _courseRepositoryMock
            .Setup(x => x.GetByIdAsync(_courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Course?)null);

        var query = new GetEnrollmentsQuery(_courseId);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _handler.Handle(query, CancellationToken.None)
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

        var query = new GetEnrollmentsQuery(_courseId);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _handler.Handle(query, CancellationToken.None)
        );
    }

    [Fact]
    public async Task Handle_WithoutTenantContext_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _tenantContextMock.Setup(x => x.TenantId).Returns((Guid?)null);
        var query = new GetEnrollmentsQuery(_courseId);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _handler.Handle(query, CancellationToken.None)
        );
    }

    [Fact]
    public async Task Handle_WithNoEnrollments_ReturnsEmptyList()
    {
        // Arrange
        var course = new Course(_tenantId, "Test Course", "Test Description");

        _courseRepositoryMock
            .Setup(x => x.GetByIdAsync(_courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(course);

        _enrollmentRepositoryMock
            .Setup(x => x.GetAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Enrollment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Enrollment>());

        var query = new GetEnrollmentsQuery(_courseId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task Handle_WithCompletedEnrollment_ReturnsCompletedAt()
    {
        // Arrange
        var course = new Course(_tenantId, "Test Course", "Test Description");

        var studentId = Guid.NewGuid();
        var student = new User("john.doe@example.com", "hashedpassword", "John", "Doe", Domain.Enums.RoleType.Student, _tenantId);

        var completedAt = DateTime.UtcNow.AddDays(-1);
        var enrollment = new Enrollment
        {
            StudentId = studentId,
            CourseId = _courseId,
            TenantId = _tenantId,
            EnrolledAt = DateTime.UtcNow.AddDays(-10),
            CompletedAt = completedAt
        };

        _courseRepositoryMock
            .Setup(x => x.GetByIdAsync(_courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(course);

        _enrollmentRepositoryMock
            .Setup(x => x.GetAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Enrollment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Enrollment> { enrollment });

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(student);

        var query = new GetEnrollmentsQuery(_courseId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.NotNull(result[0].CompletedAt);
        Assert.Equal(completedAt, result[0].CompletedAt);
    }

    [Fact]
    public async Task Handle_WithMultipleEnrollments_ReturnsOrderedByEnrolledAt()
    {
        // Arrange
        var course = new Course(_tenantId, "Test Course", "Test Description");

        var studentId1 = Guid.NewGuid();
        var studentId2 = Guid.NewGuid();
        var studentId3 = Guid.NewGuid();

        var student1 = new User("alice@example.com", "hashedpassword", "Alice", "A", Domain.Enums.RoleType.Student, _tenantId);
        var student2 = new User("bob@example.com", "hashedpassword", "Bob", "B", Domain.Enums.RoleType.Student, _tenantId);
        var student3 = new User("charlie@example.com", "hashedpassword", "Charlie", "C", Domain.Enums.RoleType.Student, _tenantId);

        var enrollment1 = new Enrollment
        {
            StudentId = studentId1,
            CourseId = _courseId,
            TenantId = _tenantId,
            EnrolledAt = DateTime.UtcNow.AddDays(-3)
        };

        var enrollment2 = new Enrollment
        {
            StudentId = studentId2,
            CourseId = _courseId,
            TenantId = _tenantId,
            EnrolledAt = DateTime.UtcNow.AddDays(-1)
        };

        var enrollment3 = new Enrollment
        {
            StudentId = studentId3,
            CourseId = _courseId,
            TenantId = _tenantId,
            EnrolledAt = DateTime.UtcNow.AddDays(-2)
        };

        _courseRepositoryMock
            .Setup(x => x.GetByIdAsync(_courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(course);

        _enrollmentRepositoryMock
            .Setup(x => x.GetAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Enrollment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Enrollment> { enrollment2, enrollment3, enrollment1 }); // Unordered

        _userRepositoryMock.Setup(x => x.GetByIdAsync(studentId1, It.IsAny<CancellationToken>())).ReturnsAsync(student1);
        _userRepositoryMock.Setup(x => x.GetByIdAsync(studentId2, It.IsAny<CancellationToken>())).ReturnsAsync(student2);
        _userRepositoryMock.Setup(x => x.GetByIdAsync(studentId3, It.IsAny<CancellationToken>())).ReturnsAsync(student3);

        var query = new GetEnrollmentsQuery(_courseId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Equal("Alice", result[0].StudentFirstName); // Enrolled first
        Assert.Equal("Charlie", result[1].StudentFirstName); // Enrolled second
        Assert.Equal("Bob", result[2].StudentFirstName); // Enrolled last
    }
}
