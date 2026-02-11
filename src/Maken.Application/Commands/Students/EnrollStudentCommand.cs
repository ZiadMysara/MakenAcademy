using MediatR;

namespace Maken.Application.Commands.Students;

/// <summary>
/// Command to enroll a student in a course.
/// </summary>
public record EnrollStudentCommand(
    Guid StudentId,
    Guid CourseId
) : IRequest<EnrollStudentResult>;

/// <summary>
/// Result of enrolling a student in a course.
/// </summary>
public record EnrollStudentResult(
    Guid Id,
    Guid StudentId,
    Guid CourseId,
    Guid TenantId,
    DateTime EnrolledAt
);
