using MediatR;

namespace Maken.Application.Commands.Students;

/// <summary>
/// Command to mark a lesson as completed by a student.
/// </summary>
public record CompleteLessonCommand(
    Guid StudentId,
    Guid LessonId
) : IRequest<Unit>;
