using Maken.Application.Common.Interfaces;
using Maken.Application.DTOs;
using Maken.Domain.Entities;
using MediatR;

namespace Maken.Application.Queries.Courses;

/// <summary>
/// Handler for GetCoursesQuery that retrieves a paginated list of courses.
/// </summary>
public sealed class GetCoursesQueryHandler : IRequestHandler<GetCoursesQuery, GetCoursesResult>
{
    private readonly IRepository<Course> _courseRepository;
    private readonly ITenantContext _tenantContext;

    public GetCoursesQueryHandler(
        IRepository<Course> courseRepository,
        ITenantContext tenantContext)
    {
        _courseRepository = courseRepository;
        _tenantContext = tenantContext;
    }

    public async Task<GetCoursesResult> Handle(GetCoursesQuery request, CancellationToken cancellationToken)
    {
        // Get current tenant ID from context
        var tenantId = _tenantContext.TenantId 
            ?? throw new UnauthorizedAccessException("Tenant context is required.");

        // Get all courses (tenant-scoped via global query filter)
        var allCourses = await _courseRepository.GetAllAsync(cancellationToken: cancellationToken);

        // Calculate pagination
        var totalCount = allCourses.Count;
        var skip = (request.PageNumber - 1) * request.PageSize;
        var courses = allCourses
            .Skip(skip)
            .Take(request.PageSize)
            .ToList();

        // Map to DTOs
        var courseDtos = courses.Select(course => new CourseDto(
            Id: course.Id,
            TenantId: course.TenantId,
            Name: course.Name,
            Description: course.Description,
            Status: course.Status.ToString(),
            FreeFlowMode: course.FreeFlowMode,
            PrerequisiteCourseIds: course.PrerequisiteCourseIds,
            CreatedAt: course.CreatedAt,
            UpdatedAt: course.UpdatedAt
        )).ToList();

        // Return result
        return new GetCoursesResult(
            Courses: courseDtos,
            TotalCount: totalCount,
            PageNumber: request.PageNumber,
            PageSize: request.PageSize
        );
    }
}
