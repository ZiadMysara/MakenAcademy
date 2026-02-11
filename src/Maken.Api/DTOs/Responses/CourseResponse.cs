namespace Maken.Api.DTOs.Responses;

/// <summary>
/// Response DTO for course details.
/// </summary>
public record CourseResponse(
    Guid Id,
    string Name,
    string Description,
    string Status,
    bool FreeFlowMode,
    List<Guid> PrerequisiteCourseIds,
    bool IsLocked,
    decimal CompletionPercentage,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
