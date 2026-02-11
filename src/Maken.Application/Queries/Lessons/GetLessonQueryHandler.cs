using Maken.Application.Common.Interfaces;
using Maken.Domain.Entities;
using MediatR;

namespace Maken.Application.Queries.Lessons;

/// <summary>
/// Handler for GetLessonQuery that retrieves a single lesson.
/// </summary>
public sealed class GetLessonQueryHandler : IRequestHandler<GetLessonQuery, Lesson>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;

    public GetLessonQueryHandler(
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext)
    {
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
    }

    public async Task<Lesson> Handle(GetLessonQuery request, CancellationToken cancellationToken)
    {
        // Get current tenant ID from context
        var tenantId = _tenantContext.TenantId 
            ?? throw new UnauthorizedAccessException("Tenant context is required.");

        // Get lesson repository
        var lessonRepository = _unitOfWork.Repository<Lesson>();

        // Find lesson by ID (tenant-scoped via global query filter)
        var lesson = await lessonRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Lesson with ID {request.Id} not found.");

        // Verify tenant ownership (additional check)
        if (lesson.TenantId != tenantId)
        {
            throw new UnauthorizedAccessException("Cannot access lesson from another tenant.");
        }

        return lesson;
    }
}
