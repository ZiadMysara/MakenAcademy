namespace Maken.Api.DTOs.Responses;

/// <summary>
/// Response DTO for lesson details.
/// </summary>
public record LessonResponse(
    Guid Id,
    Guid CourseId,
    string Title,
    string Description,
    string ContentType,
    string ContentUrl,
    int Order,
    bool IsLocked,
    bool IsCompleted,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
