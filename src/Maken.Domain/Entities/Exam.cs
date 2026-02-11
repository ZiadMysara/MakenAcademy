using Maken.Domain.Common;

namespace Maken.Domain.Entities;

/// <summary>
/// Represents an exam associated with a lesson.
/// Exams contain multiple-choice questions and have a pass threshold.
/// </summary>
public sealed class Exam : BaseEntity
{
    public Guid LessonId { get; private set; }
    public Guid TenantId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public int PassThreshold { get; private set; }

    // Navigation properties
    public Lesson? Lesson { get; private set; }
    public ICollection<Question> Questions { get; private set; } = new List<Question>();

    private Exam() { }

    /// <summary>
    /// Creates a new exam for a lesson.
    /// </summary>
    /// <param name="lessonId">The ID of the lesson this exam belongs to.</param>
    /// <param name="tenantId">The ID of the tenant that owns this exam.</param>
    /// <param name="title">Exam title (1-200 characters).</param>
    /// <param name="passThreshold">Pass threshold percentage (0-100).</param>
    public static Exam Create(Guid lessonId, Guid tenantId, string title, int passThreshold)
    {
        if (lessonId == Guid.Empty)
            throw new ArgumentException("Lesson ID cannot be empty", nameof(lessonId));
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty", nameof(title));
        if (title.Length > 200)
            throw new ArgumentException("Title cannot exceed 200 characters", nameof(title));
        if (passThreshold < 0 || passThreshold > 100)
            throw new ArgumentException("Pass threshold must be between 0 and 100", nameof(passThreshold));

        return new Exam
        {
            LessonId = lessonId,
            TenantId = tenantId,
            Title = title.Trim(),
            PassThreshold = passThreshold
        };
    }

    /// <summary>
    /// Updates the exam title.
    /// </summary>
    public void SetTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty", nameof(title));
        if (title.Length > 200)
            throw new ArgumentException("Title cannot exceed 200 characters", nameof(title));

        Title = title.Trim();
    }

    /// <summary>
    /// Updates the pass threshold.
    /// </summary>
    public void SetPassThreshold(int passThreshold)
    {
        if (passThreshold < 0 || passThreshold > 100)
            throw new ArgumentException("Pass threshold must be between 0 and 100", nameof(passThreshold));

        PassThreshold = passThreshold;
    }
}
