using Maken.Application.Common.Interfaces;
using Maken.Domain.Entities;
using Maken.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Maken.Application.Commands.Lessons;

/// <summary>
/// Handler for UpdateLessonCommand that updates an existing lesson.
/// </summary>
public sealed class UpdateLessonCommandHandler : IRequestHandler<UpdateLessonCommand, UpdateLessonResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<UpdateLessonCommandHandler> _logger;

    public UpdateLessonCommandHandler(
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext,
        ILogger<UpdateLessonCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<UpdateLessonResult> Handle(UpdateLessonCommand request, CancellationToken cancellationToken)
    {
        // Get current tenant ID from context
        var tenantId = _tenantContext.TenantId 
            ?? throw new UnauthorizedAccessException("Tenant context is required.");

        _logger.LogInformation("Updating lesson {LessonId} in tenant {TenantId}", request.Id, tenantId);

        // Get lesson repository
        var lessonRepository = _unitOfWork.Repository<Lesson>();

        // Find lesson by ID (tenant-scoped via global query filter)
        var lesson = await lessonRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Lesson with ID {request.Id} not found.");

        // Verify tenant ownership
        if (lesson.TenantId != tenantId)
        {
            _logger.LogWarning("Unauthorized attempt to update lesson {LessonId} from tenant {TenantId}", request.Id, tenantId);
            throw new UnauthorizedAccessException("Cannot update lesson from another tenant.");
        }

        // Parse content type
        if (!Enum.TryParse<ContentType>(request.ContentType, true, out var contentType))
        {
            throw new ArgumentException($"Invalid content type: {request.ContentType}");
        }

        // Update lesson properties
        lesson.SetTitle(request.Title);
        lesson.SetDescription(request.Description);
        lesson.UpdateContent(contentType, request.ContentUrl);
        lesson.SetOrder(request.Order);

        // Update entity (marks as modified)
        lessonRepository.Update(lesson);

        // Persist changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully updated lesson {LessonId}", lesson.Id);

        // Return result
        return new UpdateLessonResult(
            Id: lesson.Id,
            Title: lesson.Title,
            Description: lesson.Description,
            ContentType: lesson.ContentType.ToString(),
            ContentUrl: lesson.ContentUrl,
            Order: lesson.Order,
            UpdatedAt: lesson.UpdatedAt ?? DateTime.UtcNow
        );
    }
}
