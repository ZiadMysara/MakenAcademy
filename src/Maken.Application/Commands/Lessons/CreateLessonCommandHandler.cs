using Maken.Application.Common.Interfaces;
using Maken.Domain.Entities;
using Maken.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Maken.Application.Commands.Lessons;

/// <summary>
/// Handler for CreateLessonCommand that creates a new lesson in a course.
/// </summary>
public sealed class CreateLessonCommandHandler : IRequestHandler<CreateLessonCommand, CreateLessonResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<CreateLessonCommandHandler> _logger;

    public CreateLessonCommandHandler(
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext,
        ILogger<CreateLessonCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<CreateLessonResult> Handle(CreateLessonCommand request, CancellationToken cancellationToken)
    {
        // Get current tenant ID from context
        var tenantId = _tenantContext.TenantId 
            ?? throw new UnauthorizedAccessException("Tenant context is required.");

        _logger.LogInformation(
            "Creating lesson {LessonTitle} for course {CourseId} in tenant {TenantId}",
            request.Title,
            request.CourseId,
            tenantId);

        // Verify course exists and belongs to tenant
        var courseRepository = _unitOfWork.Repository<Course>();
        var course = await courseRepository.GetByIdAsync(request.CourseId, cancellationToken)
            ?? throw new KeyNotFoundException($"Course with ID {request.CourseId} not found.");

        if (course.TenantId != tenantId)
        {
            throw new UnauthorizedAccessException("Cannot create lesson for course from another tenant.");
        }

        // Parse content type
        if (!Enum.TryParse<ContentType>(request.ContentType, true, out var contentType))
        {
            throw new ArgumentException($"Invalid content type: {request.ContentType}");
        }

        // Create lesson entity
        var lesson = Lesson.Create(
            courseId: request.CourseId,
            tenantId: tenantId,
            title: request.Title,
            description: request.Description,
            contentType: contentType,
            contentUrl: request.ContentUrl,
            order: request.Order
        );

        // Persist to database
        var lessonRepository = _unitOfWork.Repository<Lesson>();
        await lessonRepository.AddAsync(lesson, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Successfully created lesson {LessonId} with title {LessonTitle} for course {CourseId}",
            lesson.Id,
            lesson.Title,
            lesson.CourseId);

        // Return result
        return new CreateLessonResult(
            Id: lesson.Id,
            CourseId: lesson.CourseId,
            TenantId: lesson.TenantId,
            Title: lesson.Title,
            Description: lesson.Description,
            ContentType: lesson.ContentType.ToString(),
            ContentUrl: lesson.ContentUrl,
            Order: lesson.Order,
            CreatedAt: lesson.CreatedAt
        );
    }
}
