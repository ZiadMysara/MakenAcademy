using Maken.Application.DTOs;
using MediatR;

namespace Maken.Application.Queries.Enrollments;

/// <summary>
/// Query to get all enrollments for a specific course.
/// </summary>
public record GetEnrollmentsQuery(
    Guid CourseId
) : IRequest<List<EnrollmentDto>>;
