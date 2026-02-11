using Maken.Application.Common.Interfaces;
using Maken.Application.DTOs;
using Maken.Domain.Entities;
using MediatR;

namespace Maken.Application.Queries.Courses;

/// <summary>
/// Handler for GetCourseQuery that retrieves a single course by ID.
/// </summary>
public sealed class GetCourseQueryHandler : IRequestHandler<GetCourseQuery, CourseDto?>
{
    private readonly IRepository<Course> _courseRepository;
    private readonly ITenantContext _tenantContext;

    public GetCourseQueryHandler(
        IRepository<Course> courseRepository,
        ITenantContext tenantContext)
    {
        _courseRepository = courseRepository;
        _tenantContext = tenantContext;
    }

    public async Task<CourseDto?> Handle(GetCourseQuery request, CancellationToken cancellationToken)
    {
        // Get current tenant ID from context
        var tenantId = _tenantContext.TenantId 
            ?? throw new UnauthorizedAccessException("Tenant context is required.");

        // Find course by ID (tenant-scoped via global query filter)
        var course = await _courseRepository.GetByIdAsync(request.Id, cancellationToken);

        if (course == null)
        {
            return null;
        }

        // Verify tenant ownership (additional check beyond global filter)
        if (course.TenantId != tenantId)
        {
            return null; // Return null for courses from other tenants (404 behavior)
        }

        // Map to DTO
        return new CourseDto(
            Id: course.Id,
            TenantId: course.TenantId,
            Name: course.Name,
            Description: course.Description,
            Status: course.Status.ToString(),
            FreeFlowMode: course.FreeFlowMode,
            PrerequisiteCourseIds: course.PrerequisiteCourseIds,
            CreatedAt: course.CreatedAt,
            UpdatedAt: course.UpdatedAt
        );
    }
}
