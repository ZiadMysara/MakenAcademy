namespace Maken.Application.DTOs;

/// <summary>
/// Data transfer object for lesson information.
/// </summary>
public sealed record LessonDto(
    Guid Id,
    Guid CourseId,
    string Title,
    string Description,
    string ContentType,
    string ContentUrl,
    int Order,
    bool HasExam,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    bool? IsLocked = null,
    bool? IsCompleted = null
);
