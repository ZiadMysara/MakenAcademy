using MediatR;

namespace Maken.Application.Commands.Courses;

/// <summary>
/// Command to soft delete a course.
/// Cascades to lessons, exams, enrollments, and progress records.
/// </summary>
public sealed record DeleteCourseCommand(
    Guid Id
) : IRequest<DeleteCourseResult>;

/// <summary>
/// Result of a course deletion operation.
/// </summary>
public sealed record DeleteCourseResult(
    Guid Id,
    DateTime DeletedAt
);
