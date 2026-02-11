namespace Maken.Api.DTOs.Responses;

/// <summary>
/// Response DTO for enrollment details.
/// </summary>
public record EnrollmentResponse(
    Guid Id,
    Guid StudentId,
    string StudentFirstName,
    string StudentLastName,
    string StudentEmail,
    Guid CourseId,
    DateTime EnrolledAt,
    DateTime? CompletedAt
);
