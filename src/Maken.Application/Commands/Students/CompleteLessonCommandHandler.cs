using MediatR;
using Maken.Application.Common.Interfaces;
using Maken.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Maken.Application.Commands.Students;

/// <summary>
/// Handler for marking a lesson as completed.
/// Creates or updates the progress record for the student.
/// </summary>
public class CompleteLessonCommandHandler : IRequestHandler<CompleteLessonCommand, Unit>
{
    private readonly IProgressRepository _progressRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CompleteLessonCommandHandler> _logger;

    public CompleteLessonCommandHandler(
        IProgressRepository progressRepository,
        ITenantContext tenantContext,
        IUnitOfWork unitOfWork,
        ILogger<CompleteLessonCommandHandler> logger)
    {
        _progressRepository = progressRepository;
        _tenantContext = tenantContext;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Unit> Handle(CompleteLessonCommand request, CancellationToken cancellationToken)
    {
        // Ensure tenant context is available
        if (!_tenantContext.TenantId.HasValue)
            throw new InvalidOperationException("Tenant context is required for this operation.");

        _logger.LogInformation("Marking lesson {LessonId} as completed for student {StudentId} in tenant {TenantId}", 
            request.LessonId, request.StudentId, _tenantContext.TenantId.Value);

        // Check if progress record already exists
        var existingProgress = await _progressRepository.GetByStudentAndLessonAsync(
            request.StudentId,
            request.LessonId,
            cancellationToken);

        if (existingProgress != null)
        {
            // Update existing progress
            existingProgress.MarkAsCompleted();
            _progressRepository.Update(existingProgress);
            _logger.LogInformation("Updated existing progress record for student {StudentId}, lesson {LessonId}", 
                request.StudentId, request.LessonId);
        }
        else
        {
            // Create new progress record
            var progress = Progress.Create(
                request.StudentId,
                request.LessonId,
                _tenantContext.TenantId.Value);

            progress.MarkAsCompleted();

            await _progressRepository.AddAsync(progress, cancellationToken);
            _logger.LogInformation("Created new progress record for student {StudentId}, lesson {LessonId}", 
                request.StudentId, request.LessonId);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
