namespace Maken.Domain.Enums;

/// <summary>
/// Represents the publication status of a course.
/// </summary>
public enum CourseStatus
{
    /// <summary>
    /// Course is in draft mode and not visible to students.
    /// </summary>
    Draft = 0,

    /// <summary>
    /// Course is published and visible to students.
    /// </summary>
    Published = 1
}
