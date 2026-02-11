using MediatR;

namespace Maken.Application.Commands.Courses;

/// <summary>
/// Command to create a new course.
/// </summary>
public sealed record CreateCourseCommand(
    string Name,
    string Description,
    bool FreeFlowMode = false,
    List<Guid>? PrerequisiteCourseIds = null
) : IRequest<CreateCourseResult>;

/// <summary>
/// Result of a course creation operation.
/// </summary>
public sealed record CreateCourseResult(
    Guid Id,
    Guid TenantId,
    string Name,
    string Description,
    string Status,
    bool FreeFlowMode,
    List<Guid> PrerequisiteCourseIds,
    DateTime CreatedAt
);
