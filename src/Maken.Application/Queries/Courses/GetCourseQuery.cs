using Maken.Application.DTOs;
using MediatR;

namespace Maken.Application.Queries.Courses;

/// <summary>
/// Query to get a single course by ID.
/// </summary>
public sealed record GetCourseQuery(
    Guid Id
) : IRequest<CourseDto?>;
