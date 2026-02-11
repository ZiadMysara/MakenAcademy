using Maken.Domain.Common;

namespace Maken.Domain.Entities;

/// <summary>
/// Represents a student's enrollment in a course.
/// Tracks enrollment date and completion status.
/// </summary>
public class Enrollment : BaseEntity
{
    /// <summary>
    /// Gets or sets the ID of the student enrolled in the course.
    /// </summary>
    public Guid StudentId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the course the student is enrolled in.
    /// </summary>
    public Guid CourseId { get; set; }

    /// <summary>
    /// Gets or sets the tenant ID for multi-tenancy support.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the student enrolled in the course.
    /// </summary>
    public DateTime EnrolledAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the student completed the course.
    /// Null if the course is not yet completed.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    // Navigation properties
    /// <summary>
    /// Gets or sets the student associated with this enrollment.
    /// </summary>
    public virtual User Student { get; set; } = null!;

    /// <summary>
    /// Gets or sets the course associated with this enrollment.
    /// </summary>
    public virtual Course Course { get; set; } = null!;

    /// <summary>
    /// Marks the course as completed.
    /// </summary>
    public void MarkAsCompleted()
    {
        if (CompletedAt.HasValue)
        {
            throw new InvalidOperationException("Course is already marked as completed.");
        }

        CompletedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if the course is completed.
    /// </summary>
    public bool IsCompleted() => CompletedAt.HasValue;
}
