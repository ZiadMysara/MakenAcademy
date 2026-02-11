namespace Maken.Application.DTOs;

/// <summary>
/// Data transfer object for enrollment information.
/// </summary>
public record EnrollmentDto(
    Guid Id,
    Guid StudentId,
    Guid CourseId,
    Guid TenantId,
    DateTime EnrolledAt,
    DateTime? CompletedAt,
    string StudentFirstName,
    string StudentLastName,
    string StudentEmail
);
