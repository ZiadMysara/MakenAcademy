using Maken.Application.Common.Interfaces;
using Maken.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Maken.Application.Commands.Lessons;

/// <summary>
/// Handler for ReorderLessonsCommand that updates lesson order within a course.
/// </summary>
public sealed class ReorderLessonsCommandHandler : IRequestHandler<ReorderLessonsCommand, Unit>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<ReorderLessonsCommandHandler> _logger;

    public ReorderLessonsCommandHandler(
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext,
        ILogger<ReorderLessonsCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<Unit> Handle(ReorderLessonsCommand request, CancellationToken cancellationToken)
    {
        // Get current tenant ID from context
        var tenantId = _tenantContext.TenantId 
            ?? throw new UnauthorizedAccessException("Tenant context is required.");

        _logger.LogInformation("Reordering {LessonCount} lessons for course {CourseId} in tenant {TenantId}", 
            request.LessonOrders.Count, request.CourseId, tenantId);

        // Verify course exists and belongs to tenant
        var courseRepository = _unitOfWork.Repository<Course>();
        var course = await courseRepository.GetByIdAsync(request.CourseId, cancellationToken)
            ?? throw new KeyNotFoundException($"Course with ID {request.CourseId} not found.");

        if (course.TenantId != tenantId)
        {
            _logger.LogWarning("Unauthorized attempt to reorder lessons for course {CourseId} from tenant {TenantId}", 
                request.CourseId, tenantId);
            throw new UnauthorizedAccessException("Cannot reorder lessons for course from another tenant.");
        }

        // Get lesson repository
        var lessonRepository = _unitOfWork.Repository<Lesson>();

        // Update each lesson's order
        foreach (var orderUpdate in request.LessonOrders)
        {
            var lesson = await lessonRepository.GetByIdAsync(orderUpdate.LessonId, cancellationToken)
                ?? throw new KeyNotFoundException($"Lesson with ID {orderUpdate.LessonId} not found.");

            // Verify lesson belongs to the course
            if (lesson.CourseId != request.CourseId)
            {
                throw new InvalidOperationException($"Lesson {orderUpdate.LessonId} does not belong to course {request.CourseId}.");
            }

            // Verify tenant ownership
            if (lesson.TenantId != tenantId)
            {
                throw new UnauthorizedAccessException("Cannot reorder lesson from another tenant.");
            }

            // Update order
            lesson.SetOrder(orderUpdate.NewOrder);
            lessonRepository.Update(lesson);
        }

        // Persist changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully reordered lessons for course {CourseId}", request.CourseId);

        return Unit.Value;
    }
}
