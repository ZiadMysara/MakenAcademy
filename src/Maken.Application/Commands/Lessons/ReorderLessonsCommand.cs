using MediatR;

namespace Maken.Application.Commands.Lessons;

/// <summary>
/// Command to reorder lessons within a course.
/// </summary>
public sealed record ReorderLessonsCommand(
    Guid CourseId,
    List<LessonOrderUpdate> LessonOrders
) : IRequest<Unit>;

/// <summary>
/// Represents a lesson order update.
/// </summary>
public sealed record LessonOrderUpdate(
    Guid LessonId,
    int NewOrder
);
