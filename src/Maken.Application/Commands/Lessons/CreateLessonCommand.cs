using MediatR;

namespace Maken.Application.Commands.Lessons;

/// <summary>
/// Command to create a new lesson in a course.
/// </summary>
public sealed record CreateLessonCommand(
    Guid CourseId,
    string Title,
    string Description,
    string ContentType,
    string ContentUrl,
    int Order
) : IRequest<CreateLessonResult>;

/// <summary>
/// Result of a lesson creation operation.
/// </summary>
public sealed record CreateLessonResult(
    Guid Id,
    Guid CourseId,
    Guid TenantId,
    string Title,
    string Description,
    string ContentType,
    string ContentUrl,
    int Order,
    DateTime CreatedAt
);
