namespace Maken.Api.DTOs.Requests;

/// <summary>
/// Request DTO for creating a new lesson.
/// </summary>
public record CreateLessonRequest(
    string Title,
    string Description,
    string ContentType,
    string ContentUrl,
    int Order
);
