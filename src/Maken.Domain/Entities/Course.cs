using Maken.Domain.Common;
using Maken.Domain.Enums;

namespace Maken.Domain.Entities;

/// <summary>
/// Represents a learning course in the Maken platform.
/// Courses contain lessons, can have prerequisites, and support Free Flow mode.
/// </summary>
/// <remarks>
/// Constitution requirements:
/// - Courses are tenant-scoped (multi-tenant isolation)
/// - Soft delete with cascade to lessons, exams, and enrollments
/// - Progression rules enforced unless Free Flow mode enabled
/// - Prerequisites must be completed before course access
/// </remarks>
public class Course : TenantScopedEntity
{
    /// <summary>
    /// Course name (1-200 characters).
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Course description (1-2000 characters).
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Course status (Draft or Published).
    /// Only published courses are visible to students.
    /// </summary>
    public CourseStatus Status { get; private set; }

    /// <summary>
    /// Free Flow mode flag.
    /// When enabled, all lessons unlock immediately regardless of completion.
    /// Constitution: OFF by default, can be enabled per course.
    /// </summary>
    public bool FreeFlowMode { get; private set; }

    /// <summary>
    /// List of prerequisite course IDs that must be completed before accessing this course.
    /// Stored as JSON array in database.
    /// </summary>
    public List<Guid> PrerequisiteCourseIds { get; private set; } = new();

    // Navigation properties
    /// <summary>
    /// Lessons belonging to this course.
    /// </summary>
    public ICollection<Lesson> Lessons { get; private set; } = new List<Lesson>();

    /// <summary>
    /// Student enrollments in this course.
    /// </summary>
    public ICollection<Enrollment> Enrollments { get; private set; } = new List<Enrollment>();

    /// <summary>
    /// Private constructor for EF Core.
    /// </summary>
    private Course() : base()
    {
    }

    /// <summary>
    /// Creates a new course.
    /// </summary>
    /// <param name="tenantId">The ID of the tenant that owns this course.</param>
    /// <param name="name">Course name (1-200 characters).</param>
    /// <param name="description">Course description (1-2000 characters).</param>
    /// <param name="freeFlowMode">Whether Free Flow mode is enabled (default: false).</param>
    /// <param name="prerequisiteCourseIds">Optional list of prerequisite course IDs.</param>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid.</exception>
    public Course(
        Guid tenantId,
        string name,
        string description,
        bool freeFlowMode = false,
        List<Guid>? prerequisiteCourseIds = null) : base(tenantId)
    {
        SetName(name);
        SetDescription(description);
        Status = CourseStatus.Draft; // New courses start as Draft
        FreeFlowMode = freeFlowMode;
        PrerequisiteCourseIds = prerequisiteCourseIds ?? new List<Guid>();
    }

    /// <summary>
    /// Factory method to create a new course.
    /// </summary>
    /// <param name="name">Course name (1-200 characters).</param>
    /// <param name="description">Course description (1-2000 characters).</param>
    /// <param name="tenantId">The ID of the tenant that owns this course.</param>
    /// <param name="freeFlowMode">Whether Free Flow mode is enabled (default: false).</param>
    /// <param name="prerequisiteCourseIds">Optional list of prerequisite course IDs.</param>
    /// <returns>A new Course instance.</returns>
    public static Course Create(
        string name,
        string description,
        Guid tenantId,
        bool freeFlowMode = false,
        List<Guid>? prerequisiteCourseIds = null)
    {
        return new Course(tenantId, name, description, freeFlowMode, prerequisiteCourseIds);
    }

    /// <summary>
    /// Updates the course name.
    /// </summary>
    /// <param name="name">New course name (1-200 characters).</param>
    /// <exception cref="ArgumentException">Thrown when name is invalid.</exception>
    public void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Course name cannot be empty.", nameof(name));
        }

        name = name.Trim();

        if (name.Length > 200)
        {
            throw new ArgumentException("Course name cannot exceed 200 characters.", nameof(name));
        }

        Name = name;
    }

    /// <summary>
    /// Updates the course description.
    /// </summary>
    /// <param name="description">New course description (1-2000 characters).</param>
    /// <exception cref="ArgumentException">Thrown when description is invalid.</exception>
    public void SetDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Course description cannot be empty.", nameof(description));
        }

        description = description.Trim();

        if (description.Length > 2000)
        {
            throw new ArgumentException("Course description cannot exceed 2000 characters.", nameof(description));
        }

        Description = description;
    }

    /// <summary>
    /// Publishes the course, making it visible to students.
    /// </summary>
    public void Publish()
    {
        Status = CourseStatus.Published;
    }

    /// <summary>
    /// Reverts the course to draft status, hiding it from students.
    /// </summary>
    public void Unpublish()
    {
        Status = CourseStatus.Draft;
    }

    /// <summary>
    /// Enables or disables Free Flow mode.
    /// Constitution: Free Flow mode allows all lessons to unlock immediately.
    /// </summary>
    /// <param name="enabled">Whether Free Flow mode should be enabled.</param>
    public void SetFreeFlowMode(bool enabled)
    {
        FreeFlowMode = enabled;
    }

    /// <summary>
    /// Adds a prerequisite course that must be completed before accessing this course.
    /// </summary>
    /// <param name="courseId">The ID of the prerequisite course.</param>
    /// <exception cref="ArgumentException">Thrown when courseId is invalid or already exists.</exception>
    public void AddPrerequisite(Guid courseId)
    {
        if (courseId == Guid.Empty)
        {
            throw new ArgumentException("Prerequisite course ID cannot be empty.", nameof(courseId));
        }

        if (courseId == Id)
        {
            throw new ArgumentException("Course cannot be a prerequisite of itself.", nameof(courseId));
        }

        if (PrerequisiteCourseIds.Contains(courseId))
        {
            throw new ArgumentException("Prerequisite course already exists.", nameof(courseId));
        }

        PrerequisiteCourseIds.Add(courseId);
    }

    /// <summary>
    /// Removes a prerequisite course.
    /// </summary>
    /// <param name="courseId">The ID of the prerequisite course to remove.</param>
    /// <exception cref="ArgumentException">Thrown when courseId doesn't exist in prerequisites.</exception>
    public void RemovePrerequisite(Guid courseId)
    {
        if (!PrerequisiteCourseIds.Contains(courseId))
        {
            throw new ArgumentException("Prerequisite course not found.", nameof(courseId));
        }

        PrerequisiteCourseIds.Remove(courseId);
    }

    /// <summary>
    /// Clears all prerequisite courses.
    /// </summary>
    public void ClearPrerequisites()
    {
        PrerequisiteCourseIds.Clear();
    }

    /// <summary>
    /// Checks if this course has any prerequisites.
    /// </summary>
    /// <returns>True if the course has prerequisites, false otherwise.</returns>
    public bool HasPrerequisites()
    {
        return PrerequisiteCourseIds.Any();
    }

    /// <summary>
    /// Checks if this course is published and visible to students.
    /// </summary>
    /// <returns>True if the course is published, false otherwise.</returns>
    public bool IsPublished()
    {
        return Status == CourseStatus.Published;
    }
}
