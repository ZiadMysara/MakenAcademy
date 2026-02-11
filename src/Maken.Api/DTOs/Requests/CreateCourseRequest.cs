namespace Maken.Api.DTOs.Requests;

/// <summary>
/// Request DTO for creating a new course.
/// </summary>
public record CreateCourseRequest(
    string Name,
    string Description,
    bool FreeFlowMode = false,
    List<Guid>? PrerequisiteCourseIds = null
);
