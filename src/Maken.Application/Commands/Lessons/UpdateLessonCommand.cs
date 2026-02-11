using MediatR;

namespace Maken.Application.Commands.Lessons;

/// <summary>
/// Command to update an existing lesson.
/// </summary>
public sealed record UpdateLessonCommand(
    Guid Id,
    string Title,
    string Description,
    string ContentType,
    string ContentUrl,
    int Order
) : IRequest<UpdateLessonResult>;

/// <summary>
/// Result of a lesson update operation.
/// </summary>
public sealed record UpdateLessonResult(
    Guid Id,
    string Title,
    string Description,
    string ContentType,
    string ContentUrl,
    int Order,
    DateTime UpdatedAt
);
