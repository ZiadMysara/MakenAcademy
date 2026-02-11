using Maken.Application.Common.Interfaces;
using Maken.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Maken.Application.Commands.Courses;

/// <summary>
/// Handler for CreateCourseCommand that creates a new course in the system.
/// </summary>
public sealed class CreateCourseCommandHandler : IRequestHandler<CreateCourseCommand, CreateCourseResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<CreateCourseCommandHandler> _logger;

    public CreateCourseCommandHandler(
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext,
        ILogger<CreateCourseCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<CreateCourseResult> Handle(CreateCourseCommand request, CancellationToken cancellationToken)
    {
        // Get current tenant ID from context
        Guid tenantId = _tenantContext.TenantId
            ?? throw new UnauthorizedAccessException("Tenant context is required.");

        _logger.LogInformation(
            "Creating course {CourseName} for tenant {TenantId}",
            request.Name,
            tenantId);

        // Create course entity
        Course course = new Course(
            tenantId: tenantId,
            name: request.Name,
            description: request.Description,
            freeFlowMode: request.FreeFlowMode,
            prerequisiteCourseIds: request.PrerequisiteCourseIds
        );

        // Persist to database
        IRepository<Course> courseRepository = _unitOfWork.Repository<Course>();
        await courseRepository.AddAsync(course, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Successfully created course {CourseId} with name {CourseName} for tenant {TenantId}",
            course.Id,
            course.Name,
            tenantId);

        // Return result
        return new CreateCourseResult(
            Id: course.Id,
            TenantId: course.TenantId,
            Name: course.Name,
            Description: course.Description,
            Status: course.Status.ToString(),
            FreeFlowMode: course.FreeFlowMode,
            PrerequisiteCourseIds: course.PrerequisiteCourseIds,
            CreatedAt: course.CreatedAt
        );
    }
}

