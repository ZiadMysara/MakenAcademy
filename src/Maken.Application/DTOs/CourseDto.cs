namespace Maken.Application.DTOs;

/// <summary>
/// Data Transfer Object for Course entity.
/// Used to transfer course data between layers without exposing domain entities.
/// </summary>
public sealed record CourseDto(
    Guid Id,
    Guid TenantId,
    string Name,
    string Description,
    string Status,
    bool FreeFlowMode,
    List<Guid> PrerequisiteCourseIds,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    bool? IsLocked = null,
    int? CompletionPercentage = null
);
