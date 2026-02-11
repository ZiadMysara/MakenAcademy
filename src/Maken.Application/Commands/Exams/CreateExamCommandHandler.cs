using Maken.Application.Common.Interfaces;
using Maken.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Maken.Application.Commands.Exams;

/// <summary>
/// Handler for CreateExamCommand that creates a new exam with questions and choices.
/// Validates that each question has exactly one correct answer.
/// </summary>
public sealed class CreateExamCommandHandler : IRequestHandler<CreateExamCommand, CreateExamResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<CreateExamCommandHandler> _logger;

    public CreateExamCommandHandler(
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext,
        ILogger<CreateExamCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<CreateExamResult> Handle(CreateExamCommand request, CancellationToken cancellationToken)
    {
        // Get current tenant ID from context
        Guid tenantId = _tenantContext.TenantId 
            ?? throw new UnauthorizedAccessException("Tenant context is required.");

        _logger.LogInformation("Creating exam '{Title}' for lesson {LessonId} in tenant {TenantId}", 
            request.Title, request.LessonId, tenantId);

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

        // Create exam entity
        Exam exam = Exam.Create(
            lessonId: request.LessonId,
            tenantId: tenantId,
            title: request.Title,
            passThreshold: request.PassThreshold
        );

        // Add exam to repository first to get the ID
        IRepository<Exam> examRepository = _unitOfWork.Repository<Exam>();
        await examRepository.AddAsync(exam, cancellationToken);

        // Create questions and choices
        IRepository<Question> questionRepository = _unitOfWork.Repository<Question>();
        IRepository<Choice> choiceRepository = _unitOfWork.Repository<Choice>();

        foreach (var questionDto in request.Questions)
        {
            // Create question
            Question question = Question.Create(
                examId: exam.Id,
                text: questionDto.Text,
                order: questionDto.Order
            );

            await questionRepository.AddAsync(question, cancellationToken);

            // Create choices for this question
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

        // Persist to database
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully created exam {ExamId} with {QuestionCount} questions", 
            exam.Id, request.Questions.Count);

        // Return result
        return new CreateExamResult(
            Id: exam.Id,
            LessonId: exam.LessonId,
            TenantId: exam.TenantId,
            Title: exam.Title,
            PassThreshold: exam.PassThreshold,
            CreatedAt: exam.CreatedAt
        );
    }
}
