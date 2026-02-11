using Maken.Application.Common.Interfaces;
using Maken.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Maken.Application.Commands.Lessons;

/// <summary>
/// Handler for DeleteLessonCommand that soft deletes a lesson and cascades to related entities.
/// </summary>
public sealed class DeleteLessonCommandHandler : IRequestHandler<DeleteLessonCommand, DeleteLessonResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<DeleteLessonCommandHandler> _logger;

    public DeleteLessonCommandHandler(
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext,
        ILogger<DeleteLessonCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<DeleteLessonResult> Handle(DeleteLessonCommand request, CancellationToken cancellationToken)
    {
        // Get current tenant ID from context
        var tenantId = _tenantContext.TenantId 
            ?? throw new UnauthorizedAccessException("Tenant context is required.");

        _logger.LogInformation("Deleting lesson {LessonId} in tenant {TenantId}", request.Id, tenantId);

        // Get repositories
        var lessonRepository = _unitOfWork.Repository<Lesson>();
        var examRepository = _unitOfWork.Repository<Exam>();
        var questionRepository = _unitOfWork.Repository<Question>();
        var choiceRepository = _unitOfWork.Repository<Choice>();

        // Find lesson by ID
        var lesson = await lessonRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Lesson with ID {request.Id} not found.");

        // Verify tenant ownership
        if (lesson.TenantId != tenantId)
        {
            _logger.LogWarning("Unauthorized attempt to delete lesson {LessonId} from tenant {TenantId}", request.Id, tenantId);
            throw new UnauthorizedAccessException("Cannot delete lesson from another tenant.");
        }

        // Get all exams for this lesson
        var exams = await examRepository.GetAllAsync(e => e.LessonId == request.Id, cancellationToken);

        foreach (var exam in exams)
        {
            // Get all questions for this exam
            var questions = await questionRepository.GetAllAsync(q => q.ExamId == exam.Id, cancellationToken);

            foreach (var question in questions)
            {
                // Get all choices for this question
                var choices = await choiceRepository.GetAllAsync(c => c.QuestionId == question.Id, cancellationToken);

                // Soft delete all choices
                foreach (var choice in choices)
                {
                    choice.SoftDelete();
                    choiceRepository.Update(choice);
                }

                // Soft delete question
                question.SoftDelete();
                questionRepository.Update(question);
            }

            // Soft delete exam
            exam.SoftDelete();
            examRepository.Update(exam);
        }

        // Soft delete the lesson
        lesson.SoftDelete();
        lessonRepository.Update(lesson);

        // Persist changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully deleted lesson {LessonId} with cascade", lesson.Id);

        // Return result
        return new DeleteLessonResult(
            Id: lesson.Id,
            DeletedAt: lesson.DeletedAt ?? DateTime.UtcNow
        );
    }
}
