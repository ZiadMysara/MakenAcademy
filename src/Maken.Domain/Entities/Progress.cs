using Maken.Domain.Common;

namespace Maken.Domain.Entities;

/// <summary>
/// Represents student progress on a lesson.
/// Tracks lesson completion, exam attempts, and scores.
/// </summary>
public sealed class Progress : BaseEntity
{
    public Guid StudentId { get; private set; }
    public Guid LessonId { get; private set; }
    public Guid TenantId { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public bool? ExamPassed { get; private set; }
    public int? ExamScore { get; private set; }

    // Navigation properties
    public User? Student { get; private set; }
    public Lesson? Lesson { get; private set; }

    private Progress() { }

    /// <summary>
    /// Creates a new progress record for a student viewing a lesson.
    /// </summary>
    public static Progress Create(Guid studentId, Guid lessonId, Guid tenantId)
    {
        var progress = new Progress
        {
            StudentId = studentId,
            LessonId = lessonId,
            TenantId = tenantId
        };

        // BaseEntity constructor already sets Id and CreatedAt
        // Set UpdatedBy if needed via SetUpdatedBy method
        return progress;
    }

    /// <summary>
    /// Marks the lesson as completed.
    /// </summary>
    public void MarkAsCompleted()
    {
        CompletedAt = DateTime.UtcNow;
        // UpdatedAt is handled by SetUpdatedBy method if needed
    }

    /// <summary>
    /// Records exam result for this lesson.
    /// </summary>
    public void RecordExamResult(bool passed, int score)
    {
        ExamPassed = passed;
        ExamScore = score;
        // UpdatedAt is handled by SetUpdatedBy method if needed

        // If exam is passed, mark lesson as completed
        if (passed && !CompletedAt.HasValue)
        {
            CompletedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Checks if the lesson is fully completed (viewed and exam passed if applicable).
    /// </summary>
    public bool IsFullyCompleted()
    {
        return CompletedAt.HasValue && (ExamPassed ?? true);
    }
}
