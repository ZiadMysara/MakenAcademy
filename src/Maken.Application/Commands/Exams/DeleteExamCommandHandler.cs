using Maken.Application.Common.Interfaces;
using Maken.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Maken.Application.Commands.Exams;

/// <summary>
/// Handler for DeleteExamCommand that soft deletes an exam and cascades to questions and choices.
/// </summary>
public sealed class DeleteExamCommandHandler : IRequestHandler<DeleteExamCommand, Unit>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<DeleteExamCommandHandler> _logger;

    public DeleteExamCommandHandler(
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext,
        ILogger<DeleteExamCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<Unit> Handle(DeleteExamCommand request, CancellationToken cancellationToken)
    {
        // Get current tenant ID from context
        Guid tenantId = _tenantContext.TenantId 
            ?? throw new UnauthorizedAccessException("Tenant context is required.");

        _logger.LogInformation("Deleting exam {ExamId} in tenant {TenantId}", request.Id, tenantId);

        // Get repositories
        IRepository<Exam> examRepository = _unitOfWork.Repository<Exam>();
        IRepository<Question> questionRepository = _unitOfWork.Repository<Question>();
        IRepository<Choice> choiceRepository = _unitOfWork.Repository<Choice>();

        // Find exam
        Exam? exam = await examRepository.GetByIdAsync(request.Id, cancellationToken);
        if (exam == null)
        {
            _logger.LogWarning("Exam {ExamId} not found", request.Id);
            throw new KeyNotFoundException($"Exam with ID {request.Id} not found.");
        }

        // Verify tenant ownership
        if (exam.TenantId != tenantId)
        {
            _logger.LogWarning("Unauthorized attempt to delete exam {ExamId} from tenant {TenantId}", request.Id, tenantId);
            throw new UnauthorizedAccessException("You do not have permission to delete this exam.");
        }

        // Get all questions for this exam
        var questions = await questionRepository.GetAllAsync(
            q => q.ExamId == exam.Id,
            cancellationToken);

        // Cascade soft delete to choices first
        foreach (var question in questions)
        {
            var choices = await choiceRepository.GetAllAsync(
                c => c.QuestionId == question.Id,
                cancellationToken);

            foreach (var choice in choices)
            {
                choice.SoftDelete();
                choiceRepository.Update(choice);
            }
        }

        // Then soft delete questions
        foreach (var question in questions)
        {
            question.SoftDelete();
            questionRepository.Update(question);
        }

        // Finally soft delete the exam
        exam.SoftDelete();
        examRepository.Update(exam);

        // Persist changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully deleted exam {ExamId} with cascade", exam.Id);

        return Unit.Value;
    }
}
