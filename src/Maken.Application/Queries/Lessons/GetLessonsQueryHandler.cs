using Maken.Application.Common.Interfaces;
using Maken.Domain.Entities;
using MediatR;

namespace Maken.Application.Queries.Lessons;

/// <summary>
/// Handler for GetLessonsQuery that retrieves all lessons for a course ordered by Order field.
/// </summary>
public sealed class GetLessonsQueryHandler : IRequestHandler<GetLessonsQuery, GetLessonsQueryResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;

    public GetLessonsQueryHandler(
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext)
    {
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
    }

    public async Task<GetLessonsQueryResult> Handle(GetLessonsQuery request, CancellationToken cancellationToken)
    {
        // Get current tenant ID from context
        var tenantId = _tenantContext.TenantId 
            ?? throw new UnauthorizedAccessException("Tenant context is required.");

        // Verify course exists and belongs to tenant
        var courseRepository = _unitOfWork.Repository<Course>();
        var course = await courseRepository.GetByIdAsync(request.CourseId, cancellationToken)
            ?? throw new KeyNotFoundException($"Course with ID {request.CourseId} not found.");

        if (course.TenantId != tenantId)
        {
            throw new UnauthorizedAccessException("Cannot access lessons for course from another tenant.");
        }

        // Get lessons for the course ordered by Order field
        var lessonRepository = _unitOfWork.Repository<Lesson>();
        var lessons = await lessonRepository.GetAllAsync(
            filter: l => l.CourseId == request.CourseId,
            cancellationToken: cancellationToken);
        
        // Order by Order field
        lessons = lessons.OrderBy(l => l.Order).ToList();

        return new GetLessonsQueryResult(lessons);
    }
}
