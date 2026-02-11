namespace Maken.Api.DTOs.Requests;

/// <summary>
/// Request DTO for updating an existing course.
/// </summary>
public record UpdateCourseRequest(
    string Name,
    string Description,
    bool FreeFlowMode,
    List<Guid>? PrerequisiteCourseIds = null
);
