using Maken.Domain.Common;
using Maken.Domain.Enums;

namespace Maken.Domain.Entities;

/// <summary>
/// Represents a lesson within a course.
/// Lessons are ordered sequentially and can have associated exams.
/// </summary>
public sealed class Lesson : BaseEntity
{
    private Lesson() { } // EF Core constructor

    public Lesson(
        Guid courseId,
        Guid tenantId,
        string title,
        string description,
        ContentType contentType,
        string contentUrl,
        int order)
    {
        if (courseId == Guid.Empty)
            throw new ArgumentException("Course ID cannot be empty", nameof(courseId));
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty", nameof(title));
        if (title.Length > 200)
            throw new ArgumentException("Title cannot exceed 200 characters", nameof(title));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description cannot be empty", nameof(description));
        if (description.Length > 2000)
            throw new ArgumentException("Description cannot exceed 2000 characters", nameof(description));
        if (string.IsNullOrWhiteSpace(contentUrl))
            throw new ArgumentException("Content URL cannot be empty", nameof(contentUrl));
        if (contentUrl.Length > 500)
            throw new ArgumentException("Content URL cannot exceed 500 characters", nameof(contentUrl));
        if (order < 1)
            throw new ArgumentException("Order must be a positive integer", nameof(order));

        CourseId = courseId;
        TenantId = tenantId;
        Title = title.Trim();
        Description = description.Trim();
        ContentType = contentType;
        ContentUrl = contentUrl.Trim();
        Order = order;
    }

    public Guid CourseId { get; private set; }
    public Guid TenantId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public ContentType ContentType { get; private set; }
    public string ContentUrl { get; private set; } = string.Empty;
    public int Order { get; private set; }

    // Navigation properties
    public Course? Course { get; private set; }
    public Exam? Exam { get; private set; }
    public ICollection<Progress> ProgressRecords { get; private set; } = new List<Progress>();

    // Business methods

    /// <summary>
    /// Updates the lesson order within the course.
    /// </summary>
    public void SetOrder(int order)
    {
        if (order < 1)
            throw new ArgumentException("Order must be a positive integer", nameof(order));

        Order = order;
    }

    /// <summary>
    /// Updates the lesson content type and URL.
    /// </summary>
    public void UpdateContent(ContentType contentType, string contentUrl)
    {
        if (string.IsNullOrWhiteSpace(contentUrl))
            throw new ArgumentException("Content URL cannot be empty", nameof(contentUrl));
        if (contentUrl.Length > 500)
            throw new ArgumentException("Content URL cannot exceed 500 characters", nameof(contentUrl));

        ContentType = contentType;
        ContentUrl = contentUrl.Trim();
    }

    /// <summary>
    /// Updates the lesson title.
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
    /// Updates the lesson description.
    /// </summary>
    public void SetDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description cannot be empty", nameof(description));
        if (description.Length > 2000)
            throw new ArgumentException("Description cannot exceed 2000 characters", nameof(description));

        Description = description.Trim();
    }

    /// <summary>
    /// Factory method to create a new lesson.
    /// </summary>
    public static Lesson Create(
        Guid courseId,
        Guid tenantId,
        string title,
        string description,
        ContentType contentType,
        string contentUrl,
        int order)
    {
        return new Lesson(courseId, tenantId, title, description, contentType, contentUrl, order);
    }
}
