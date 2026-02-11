using MediatR;

namespace Maken.Application.Commands.Courses;

/// <summary>
/// Command to update an existing course.
/// </summary>
public sealed record UpdateCourseCommand(
    Guid Id,
    string Name,
    string Description,
    bool FreeFlowMode,
    List<Guid>? PrerequisiteCourseIds = null
) : IRequest<UpdateCourseResult>;

/// <summary>
/// Result of a course update operation.
/// </summary>
public sealed record UpdateCourseResult(
    Guid Id,
    string Name,
    string Description,
    string Status,
    bool FreeFlowMode,
    List<Guid> PrerequisiteCourseIds,
    DateTime UpdatedAt
);
