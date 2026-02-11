using FsCheck;
using FsCheck.Xunit;
using Maken.Application.Commands.Students;
using Maken.Application.Common.Interfaces;
using Maken.Domain.Entities;
using Moq;
using Microsoft.Extensions.Logging;

namespace Maken.Application.Tests.Properties;

/// <summary>
/// Property-based tests for Enrollment operations.
/// Tests universal properties that should hold across all valid inputs.
/// Minimum 100 iterations per property test as per design.md.
/// </summary>
public class EnrollmentPropertiesTests
{
    /// <summary>
    /// Property 13: Enrollment Uniqueness
    /// For any student and course, enrolling the student should create an enrollment record. 
    /// Attempting to enroll the same student in the same course a second time should fail 
    /// with a validation error (unique constraint violation).
    /// 
    /// Feature: backend-business-features, Property 13: Enrollment Uniqueness
    /// Validates: Requirements FR-022
    /// </summary>
    [Property(MaxTest = 100)]
    public bool EnrollmentCreation_WithDuplicateStudentAndCourse_FailsWithValidationError(
        Guid tenantId,
        Guid courseId,
        Guid studentId)
    {
        // Arrange
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var tenantContextMock = new Mock<ITenantContext>();
        var courseRepositoryMock = new Mock<IRepository<Course>>();
        var userRepositoryMock = new Mock<IRepository<User>>();
        var enrollmentRepositoryMock = new Mock<IRepository<Enrollment>>();

        // Setup tenant context
        tenantContextMock.Setup(x => x.TenantId).Returns(tenantId);

        // Setup repositories
        unitOfWorkMock.Setup(x => x.Repository<Course>()).Returns(courseRepositoryMock.Object);
        unitOfWorkMock.Setup(x => x.Repository<User>()).Returns(userRepositoryMock.Object);
        unitOfWorkMock.Setup(x => x.Repository<Enrollment>()).Returns(enrollmentRepositoryMock.Object);

        // Setup course
        var course = new Course(tenantId, "Test Course", "Test Description");
        courseRepositoryMock
            .Setup(x => x.GetByIdAsync(courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(course);

        // Setup student
        var student = new User("john.doe@example.com", "hashedpassword", "John", "Doe", Domain.Enums.RoleType.Student, tenantId);
        userRepositoryMock
            .Setup(x => x.GetByIdAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(student);

        // First enrollment - no existing enrollments
        enrollmentRepositoryMock
            .Setup(x => x.GetAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Enrollment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Enrollment>());

        var handler = new EnrollStudentCommandHandler(unitOfWorkMock.Object, tenantContextMock.Object, new Mock<ILogger<EnrollStudentCommandHandler>>().Object);
        var command = new EnrollStudentCommand(studentId, courseId);

        // Act - First enrollment should succeed
        var firstResult = handler.Handle(command, CancellationToken.None).GetAwaiter().GetResult();

        // Verify first enrollment succeeded
        if (firstResult.Id == Guid.Empty || 
            firstResult.StudentId != studentId || 
            firstResult.CourseId != courseId ||
            firstResult.TenantId != tenantId)
        {
            return false; // First enrollment failed unexpectedly
        }

        // Setup for second enrollment - now there's an existing enrollment
        var existingEnrollment = new Enrollment
        {
            StudentId = studentId,
            CourseId = courseId,
            TenantId = tenantId,
            EnrolledAt = DateTime.UtcNow
        };

        enrollmentRepositoryMock
            .Setup(x => x.GetAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Enrollment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Enrollment> { existingEnrollment });

        // Act - Second enrollment should fail
        try
        {
            var secondResult = handler.Handle(command, CancellationToken.None).GetAwaiter().GetResult();
            
            // If we got here, the second enrollment succeeded when it should have failed
            return false;
        }
        catch (InvalidOperationException ex)
        {
            // Expected exception - verify it's the right error message
            return ex.Message.Contains("already enrolled", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception)
        {
            // Wrong exception type
            return false;
        }
    }

    /// <summary>
    /// Property 13 (Variant): Enrollment Creation Success
    /// For any student and course where no enrollment exists, enrolling the student should 
    /// create an enrollment record with correct properties.
    /// 
    /// Feature: backend-business-features, Property 13: Enrollment Uniqueness
    /// Validates: Requirements FR-022
    /// </summary>
    [Property(MaxTest = 100)]
    public bool EnrollmentCreation_WithUniqueStudentAndCourse_Succeeds(
        Guid tenantId,
        Guid courseId,
        Guid studentId)
    {
        // Arrange
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var tenantContextMock = new Mock<ITenantContext>();
        var courseRepositoryMock = new Mock<IRepository<Course>>();
        var userRepositoryMock = new Mock<IRepository<User>>();
        var enrollmentRepositoryMock = new Mock<IRepository<Enrollment>>();

        // Setup tenant context
        tenantContextMock.Setup(x => x.TenantId).Returns(tenantId);

        // Setup repositories
        unitOfWorkMock.Setup(x => x.Repository<Course>()).Returns(courseRepositoryMock.Object);
        unitOfWorkMock.Setup(x => x.Repository<User>()).Returns(userRepositoryMock.Object);
        unitOfWorkMock.Setup(x => x.Repository<Enrollment>()).Returns(enrollmentRepositoryMock.Object);

        // Setup course
        var course = new Course(tenantId, "Test Course", "Test Description");
        courseRepositoryMock
            .Setup(x => x.GetByIdAsync(courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(course);

        // Setup student
        var student = new User("john.doe@example.com", "hashedpassword", "John", "Doe", Domain.Enums.RoleType.Student, tenantId);
        userRepositoryMock
            .Setup(x => x.GetByIdAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(student);

        // No existing enrollments
        enrollmentRepositoryMock
            .Setup(x => x.GetAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Enrollment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Enrollment>());

        var handler = new EnrollStudentCommandHandler(unitOfWorkMock.Object, tenantContextMock.Object, new Mock<ILogger<EnrollStudentCommandHandler>>().Object);
        var command = new EnrollStudentCommand(studentId, courseId);

        var beforeEnrollment = DateTime.UtcNow;

        // Act
        var result = handler.Handle(command, CancellationToken.None).GetAwaiter().GetResult();

        // Assert
        var afterEnrollment = DateTime.UtcNow;

        return result.Id != Guid.Empty &&
               result.StudentId == studentId &&
               result.CourseId == courseId &&
               result.TenantId == tenantId &&
               result.EnrolledAt >= beforeEnrollment &&
               result.EnrolledAt <= afterEnrollment;
    }

    /// <summary>
    /// Property 13 (Variant): Different Students Can Enroll in Same Course
    /// For any course and two different students, both students should be able to enroll 
    /// in the same course successfully.
    /// 
    /// Feature: backend-business-features, Property 13: Enrollment Uniqueness
    /// Validates: Requirements FR-022
    /// </summary>
    [Property(MaxTest = 100)]
    public bool EnrollmentCreation_WithDifferentStudentsSameCourse_BothSucceed(
        Guid tenantId,
        Guid courseId,
        Guid studentId1,
        Guid studentId2)
    {
        // Skip if student IDs are the same
        if (studentId1 == studentId2)
        {
            return true;
        }

        // Arrange
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var tenantContextMock = new Mock<ITenantContext>();
        var courseRepositoryMock = new Mock<IRepository<Course>>();
        var userRepositoryMock = new Mock<IRepository<User>>();
        var enrollmentRepositoryMock = new Mock<IRepository<Enrollment>>();

        // Setup tenant context
        tenantContextMock.Setup(x => x.TenantId).Returns(tenantId);

        // Setup repositories
        unitOfWorkMock.Setup(x => x.Repository<Course>()).Returns(courseRepositoryMock.Object);
        unitOfWorkMock.Setup(x => x.Repository<User>()).Returns(userRepositoryMock.Object);
        unitOfWorkMock.Setup(x => x.Repository<Enrollment>()).Returns(enrollmentRepositoryMock.Object);

        // Setup course
        var course = new Course(tenantId, "Test Course", "Test Description");
        courseRepositoryMock
            .Setup(x => x.GetByIdAsync(courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(course);

        // Setup students
        var student1 = new User("john.doe@example.com", "hashedpassword", "John", "Doe", Domain.Enums.RoleType.Student, tenantId);
        var student2 = new User("jane.smith@example.com", "hashedpassword", "Jane", "Smith", Domain.Enums.RoleType.Student, tenantId);
        userRepositoryMock
            .Setup(x => x.GetByIdAsync(studentId1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(student1);
        userRepositoryMock
            .Setup(x => x.GetByIdAsync(studentId2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(student2);

        // First enrollment - no existing enrollments
        enrollmentRepositoryMock
            .Setup(x => x.GetAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Enrollment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Enrollment>());

        var handler = new EnrollStudentCommandHandler(unitOfWorkMock.Object, tenantContextMock.Object, new Mock<ILogger<EnrollStudentCommandHandler>>().Object);
        var command1 = new EnrollStudentCommand(studentId1, courseId);

        // Act - First student enrollment
        var result1 = handler.Handle(command1, CancellationToken.None).GetAwaiter().GetResult();

        // Verify first enrollment succeeded
        if (result1.Id == Guid.Empty || result1.StudentId != studentId1)
        {
            return false;
        }

        // Setup for second enrollment - student1 is enrolled, but student2 is not
        var enrollment1 = new Enrollment
        {
            StudentId = studentId1,
            CourseId = courseId,
            TenantId = tenantId,
            EnrolledAt = DateTime.UtcNow
        };

        // Mock should return enrollment1 only when checking for student1, empty for student2
        enrollmentRepositoryMock
            .Setup(x => x.GetAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Enrollment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((System.Linq.Expressions.Expression<Func<Enrollment, bool>> predicate, CancellationToken ct) =>
            {
                // Compile and test the predicate
                var compiledPredicate = predicate.Compile();
                var testEnrollment = new Enrollment { StudentId = studentId2, CourseId = courseId };
                
                // If the predicate is checking for student2, return empty list
                if (compiledPredicate(testEnrollment))
                {
                    return new List<Enrollment>();
                }
                
                // Otherwise return enrollment1
                return new List<Enrollment> { enrollment1 };
            });

        var command2 = new EnrollStudentCommand(studentId2, courseId);

        // Act - Second student enrollment should succeed
        var result2 = handler.Handle(command2, CancellationToken.None).GetAwaiter().GetResult();

        // Assert
        return result2.Id != Guid.Empty &&
               result2.StudentId == studentId2 &&
               result2.CourseId == courseId &&
               result2.TenantId == tenantId;
    }
}
