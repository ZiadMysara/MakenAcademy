using MediatR;

namespace Maken.Application.Commands.Courses;

/// <summary>
/// Command to publish a course, making it visible to students.
/// </summary>
public sealed record PublishCourseCommand(
    Guid Id
) : IRequest<PublishCourseResult>;

/// <summary>
/// Result of a course publish operation.
/// </summary>
public sealed record PublishCourseResult(
    Guid Id,
    string Status,
    DateTime UpdatedAt
);
