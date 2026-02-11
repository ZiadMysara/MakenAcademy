using MediatR;
using Maken.Application.DTOs;

namespace Maken.Application.Queries.Courses;

/// <summary>
/// Query to retrieve courses for a student with locked/unlocked status based on prerequisites and progression
/// </summary>
public class GetStudentCoursesQuery : IRequest<IEnumerable<CourseDto>>
{
    public Guid StudentId { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
