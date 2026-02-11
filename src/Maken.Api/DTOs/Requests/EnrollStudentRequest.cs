namespace Maken.Api.DTOs.Requests;

/// <summary>
/// Request DTO for enrolling a student in a course.
/// </summary>
public record EnrollStudentRequest(
    Guid StudentId
);
