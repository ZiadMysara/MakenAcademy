using Maken.Application.Common.Interfaces;
using Maken.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Maken.Application.Commands.Exams;

/// <summary>
/// Handler for UpdateExamCommand that updates an existing exam.
/// </summary>
public sealed class UpdateExamCommandHandler : IRequestHandler<UpdateExamCommand, UpdateExamResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<UpdateExamCommandHandler> _logger;

    public UpdateExamCommandHandler(
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext,
        ILogger<UpdateExamCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<UpdateExamResult> Handle(UpdateExamCommand request, CancellationToken cancellationToken)
    {
        // Get current tenant ID from context
        Guid tenantId = _tenantContext.TenantId 
            ?? throw new UnauthorizedAccessException("Tenant context is required.");

        _logger.LogInformation("Updating exam {ExamId} in tenant {TenantId}", request.Id, tenantId);

        // Validate that each question has exactly one correct answer
        foreach (var questionDto in request.Questions)
        {
            int correctAnswerCount = questionDto.Choices.Count(c => c.IsCorrect);
            if (correctAnswerCount != 1)
            {
                _logger.LogWarning("Validation failed: Question '{QuestionText}' has {CorrectAnswerCount} correct answers", 
                    questionDto.Text, correctAnswerCount);
                throw new InvalidOperationException(
                    $"Question '{questionDto.Text}' must have exactly one correct answer. Found {correctAnswerCount}.");
            }
        }

        // Get exam repository
        IRepository<Exam> examRepository = _unitOfWork.Repository<Exam>();
        
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
            _logger.LogWarning("Unauthorized attempt to update exam {ExamId} from tenant {TenantId}", request.Id, tenantId);
            throw new UnauthorizedAccessException("You do not have permission to update this exam.");
        }

        // Update exam properties
        exam.SetTitle(request.Title);
        exam.SetPassThreshold(request.PassThreshold);

        // For simplicity, we'll delete all existing questions and choices and recreate them
        // In a production system, you might want to update existing ones to preserve IDs
        IRepository<Question> questionRepository = _unitOfWork.Repository<Question>();
        IRepository<Choice> choiceRepository = _unitOfWork.Repository<Choice>();

        // Get existing questions for this exam
        var existingQuestions = await questionRepository.GetAllAsync(
            q => q.ExamId == exam.Id,
            cancellationToken);

        // Soft delete existing questions and choices
        foreach (var existingQuestion in existingQuestions)
        {
            var existingChoices = await choiceRepository.GetAllAsync(
                c => c.QuestionId == existingQuestion.Id,
                cancellationToken);

            foreach (var choice in existingChoices)
            {
                choice.SoftDelete();
                choiceRepository.Update(choice);
            }

            existingQuestion.SoftDelete();
            questionRepository.Update(existingQuestion);
        }

        // Create new questions and choices
        foreach (var questionDto in request.Questions)
        {
            Question question = Question.Create(
                examId: exam.Id,
                text: questionDto.Text,
                order: questionDto.Order
            );

            await questionRepository.AddAsync(question, cancellationToken);

            foreach (var choiceDto in questionDto.Choices)
            {
                Choice choice = Choice.Create(
                    questionId: question.Id,
                    text: choiceDto.Text,
                    isCorrect: choiceDto.IsCorrect
                );

                await choiceRepository.AddAsync(choice, cancellationToken);
            }
        }

        // Persist changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully updated exam {ExamId} with {QuestionCount} questions", 
            exam.Id, request.Questions.Count);

        // Return result
        return new UpdateExamResult(
            Id: exam.Id,
            LessonId: exam.LessonId,
            TenantId: exam.TenantId,
            Title: exam.Title,
            PassThreshold: exam.PassThreshold,
            UpdatedAt: exam.UpdatedAt ?? DateTime.UtcNow
        );
    }
}
