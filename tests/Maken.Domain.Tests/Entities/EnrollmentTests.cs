using Maken.Domain.Entities;
using Xunit;

namespace Maken.Domain.Tests.Entities;

/// <summary>
/// Unit tests for the Enrollment entity.
/// </summary>
public class EnrollmentTests
{
    [Fact]
    public void Enrollment_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var enrollment = new Enrollment();

        // Assert
        Assert.NotEqual(Guid.Empty, enrollment.Id); // BaseEntity auto-generates Id
        Assert.Equal(Guid.Empty, enrollment.StudentId);
        Assert.Equal(Guid.Empty, enrollment.CourseId);
        Assert.Equal(Guid.Empty, enrollment.TenantId);
        Assert.Equal(default, enrollment.EnrolledAt);
        Assert.Null(enrollment.CompletedAt);
    }

    [Fact]
    public void Enrollment_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var enrolledAt = DateTime.UtcNow;

        // Act
        var enrollment = new Enrollment
        {
            StudentId = studentId,
            CourseId = courseId,
            TenantId = tenantId,
            EnrolledAt = enrolledAt
        };

        // Assert
        Assert.Equal(studentId, enrollment.StudentId);
        Assert.Equal(courseId, enrollment.CourseId);
        Assert.Equal(tenantId, enrollment.TenantId);
        Assert.Equal(enrolledAt, enrollment.EnrolledAt);
        Assert.Null(enrollment.CompletedAt);
    }

    [Fact]
    public void MarkAsCompleted_ShouldSetCompletedAt()
    {
        // Arrange
        var enrollment = new Enrollment
        {
            StudentId = Guid.NewGuid(),
            CourseId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            EnrolledAt = DateTime.UtcNow
        };

        // Act
        enrollment.MarkAsCompleted();

        // Assert
        Assert.NotNull(enrollment.CompletedAt);
        Assert.True((DateTime.UtcNow - enrollment.CompletedAt.Value).TotalSeconds < 1);
    }

    [Fact]
    public void MarkAsCompleted_WhenAlreadyCompleted_ShouldThrowException()
    {
        // Arrange
        var enrollment = new Enrollment
        {
            StudentId = Guid.NewGuid(),
            CourseId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            EnrolledAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => enrollment.MarkAsCompleted());
        Assert.Equal("Course is already marked as completed.", exception.Message);
    }

    [Fact]
    public void IsCompleted_WhenCompletedAtIsNull_ShouldReturnFalse()
    {
        // Arrange
        var enrollment = new Enrollment
        {
            StudentId = Guid.NewGuid(),
            CourseId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            EnrolledAt = DateTime.UtcNow
        };

        // Act
        var result = enrollment.IsCompleted();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsCompleted_WhenCompletedAtHasValue_ShouldReturnTrue()
    {
        // Arrange
        var enrollment = new Enrollment
        {
            StudentId = Guid.NewGuid(),
            CourseId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            EnrolledAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow
        };

        // Act
        var result = enrollment.IsCompleted();

        // Assert
        Assert.True(result);
    }
}
