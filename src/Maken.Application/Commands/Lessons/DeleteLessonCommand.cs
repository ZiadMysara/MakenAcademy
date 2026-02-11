using MediatR;

namespace Maken.Application.Commands.Lessons;

/// <summary>
/// Command to soft delete a lesson.
/// </summary>
public sealed record DeleteLessonCommand(Guid Id) : IRequest<DeleteLessonResult>;

/// <summary>
/// Result of a lesson deletion operation.
/// </summary>
public sealed record DeleteLessonResult(
    Guid Id,
    DateTime DeletedAt
);
