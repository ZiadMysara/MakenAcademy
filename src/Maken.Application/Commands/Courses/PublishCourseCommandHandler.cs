using Maken.Application.Common.Interfaces;
using Maken.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Maken.Application.Commands.Courses;

/// <summary>
/// Handler for PublishCourseCommand that publishes a course.
/// </summary>
public sealed class PublishCourseCommandHandler : IRequestHandler<PublishCourseCommand, PublishCourseResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<PublishCourseCommandHandler> _logger;

    public PublishCourseCommandHandler(
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext,
        ILogger<PublishCourseCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<PublishCourseResult> Handle(PublishCourseCommand request, CancellationToken cancellationToken)
    {
        // Get current tenant ID from context
        var tenantId = _tenantContext.TenantId 
            ?? throw new UnauthorizedAccessException("Tenant context is required.");

        _logger.LogInformation(
            "Publishing course {CourseId} for tenant {TenantId}",
            request.Id,
            tenantId);

        // Get course repository
        var courseRepository = _unitOfWork.Repository<Course>();

        // Find course by ID (tenant-scoped via global query filter)
        var course = await courseRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Course with ID {request.Id} not found.");

        // Verify tenant ownership
        if (course.TenantId != tenantId)
        {
            throw new UnauthorizedAccessException("Cannot publish course from another tenant.");
        }

        // Publish course
        course.Publish();

        // Update entity (marks as modified)
        courseRepository.Update(course);

        // Persist changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Successfully published course {CourseId} for tenant {TenantId}",
            course.Id,
            tenantId);

        // Return result
        return new PublishCourseResult(
            Id: course.Id,
            Status: course.Status.ToString(),
            UpdatedAt: course.UpdatedAt ?? DateTime.UtcNow
        );
    }
}
