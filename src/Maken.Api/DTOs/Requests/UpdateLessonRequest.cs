namespace Maken.Api.DTOs.Requests;

/// <summary>
/// Request DTO for updating an existing lesson.
/// </summary>
public record UpdateLessonRequest(
    string Title,
    string Description,
    string ContentType,
    string ContentUrl,
    int Order
);
