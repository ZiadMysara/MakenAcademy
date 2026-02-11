using Maken.Application.Common.Interfaces;
using Maken.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Maken.Application.Commands.Courses;

/// <summary>
/// Handler for UpdateCourseCommand that updates an existing course.
/// </summary>
public sealed class UpdateCourseCommandHandler : IRequestHandler<UpdateCourseCommand, UpdateCourseResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<UpdateCourseCommandHandler> _logger;

    public UpdateCourseCommandHandler(
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext,
        ILogger<UpdateCourseCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<UpdateCourseResult> Handle(UpdateCourseCommand request, CancellationToken cancellationToken)
    {
        // Get current tenant ID from context
        var tenantId = _tenantContext.TenantId 
            ?? throw new UnauthorizedAccessException("Tenant context is required.");

        _logger.LogInformation(
            "Updating course {CourseId} for tenant {TenantId}",
            request.Id,
            tenantId);

        // Get course repository
        var courseRepository = _unitOfWork.Repository<Course>();

        // Find course by ID (tenant-scoped via global query filter)
        var course = await courseRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Course with ID {request.Id} not found.");

        // Verify tenant ownership (additional check beyond global filter)
        if (course.TenantId != tenantId)
        {
            throw new UnauthorizedAccessException("Cannot update course from another tenant.");
        }

        // Update course properties
        course.SetName(request.Name);
        course.SetDescription(request.Description);
        course.SetFreeFlowMode(request.FreeFlowMode);

        // Update prerequisites
        course.ClearPrerequisites();
        if (request.PrerequisiteCourseIds != null)
        {
            foreach (var prerequisiteId in request.PrerequisiteCourseIds)
            {
                course.AddPrerequisite(prerequisiteId);
            }
        }

        // Update entity (marks as modified)
        courseRepository.Update(course);

        // Persist changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Successfully updated course {CourseId} with name {CourseName} for tenant {TenantId}",
            course.Id,
            course.Name,
            tenantId);

        // Return result
        return new UpdateCourseResult(
            Id: course.Id,
            Name: course.Name,
            Description: course.Description,
            Status: course.Status.ToString(),
            FreeFlowMode: course.FreeFlowMode,
            PrerequisiteCourseIds: course.PrerequisiteCourseIds,
            UpdatedAt: course.UpdatedAt ?? DateTime.UtcNow
        );
    }
}
