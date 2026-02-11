using MediatR;

namespace Maken.Application.Queries.Courses;

/// <summary>
/// Query to get a paginated list of courses.
/// </summary>
public sealed record GetCoursesQuery(
    int PageNumber = 1,
    int PageSize = 20
) : IRequest<GetCoursesResult>;

/// <summary>
/// Result of a get courses query with pagination.
/// </summary>
public sealed record GetCoursesResult(
    List<Maken.Application.DTOs.CourseDto> Courses,
    int TotalCount,
    int PageNumber,
    int PageSize
);
