using Maken.Application.Common.Interfaces;
using Maken.Domain.Entities;
using Maken.Domain.Services;
using Maken.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Maken.Application.Commands.Exams;

/// <summary>
/// Handler for SubmitExamCommand that grades the exam and updates progress.
/// </summary>
public sealed class SubmitExamCommandHandler : IRequestHandler<SubmitExamCommand, ExamResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;
    private readonly ExamGradingService _gradingService;
    private readonly ILogger<SubmitExamCommandHandler> _logger;

    public SubmitExamCommandHandler(
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext,
        ExamGradingService gradingService,
        ILogger<SubmitExamCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
        _gradingService = gradingService;
        _logger = logger;
    }

    public async Task<ExamResult> Handle(SubmitExamCommand request, CancellationToken cancellationToken)
    {
        // Get current tenant ID from context
        Guid tenantId = _tenantContext.TenantId 
            ?? throw new UnauthorizedAccessException("Tenant context is required.");

        _logger.LogInformation("Student {StudentId} submitting exam {ExamId} in tenant {TenantId}", 
            request.StudentId, request.ExamId, tenantId);

        // Get repositories
        IRepository<Exam> examRepository = _unitOfWork.Repository<Exam>();
        IRepository<Question> questionRepository = _unitOfWork.Repository<Question>();
        IRepository<Choice> choiceRepository = _unitOfWork.Repository<Choice>();
        IRepository<Progress> progressRepository = _unitOfWork.Repository<Progress>();
        IRepository<Lesson> lessonRepository = _unitOfWork.Repository<Lesson>();

        // Find exam
        Exam? exam = await examRepository.GetByIdAsync(request.ExamId, cancellationToken);
        if (exam == null)
        {
            _logger.LogWarning("Exam {ExamId} not found", request.ExamId);
            throw new KeyNotFoundException($"Exam with ID {request.ExamId} not found.");
        }

        // Verify tenant ownership
        if (exam.TenantId != tenantId)
        {
            _logger.LogWarning("Unauthorized attempt to access exam {ExamId} from tenant {TenantId}", request.ExamId, tenantId);
            throw new UnauthorizedAccessException("You do not have permission to access this exam.");
        }

        // Get all questions for this exam
        var questions = await questionRepository.GetAllAsync(
            q => q.ExamId == exam.Id,
            cancellationToken);

        // Get all choices for these questions
        var questionIds = questions.Select(q => q.Id).ToList();
        var allChoices = await choiceRepository.GetAllAsync(
            c => questionIds.Contains(c.QuestionId),
            cancellationToken);

        // Grade the exam
        int correctAnswers = 0;
        foreach (var answer in request.Answers)
        {
            var choice = allChoices.FirstOrDefault(c => c.Id == answer.SelectedChoiceId);
            if (choice != null && choice.IsCorrect)
            {
                correctAnswers++;
            }
        }

        var result = _gradingService.GradeExam(
            totalQuestions: questions.Count,
            correctAnswers: correctAnswers,
            passThreshold: exam.PassThreshold
        );

        _logger.LogInformation("Exam {ExamId} graded: {CorrectAnswers}/{TotalQuestions}, Score: {Score}, Passed: {Passed}", 
            exam.Id, correctAnswers, questions.Count, result.Score, result.Passed);

        // Update or create progress record
        var existingProgress = (await progressRepository.GetAllAsync(
            p => p.StudentId == request.StudentId && p.LessonId == exam.LessonId,
            cancellationToken)).FirstOrDefault();

        if (existingProgress != null)
        {
            // Update existing progress
            existingProgress.RecordExamResult(result.Passed, result.Score);
            progressRepository.Update(existingProgress);
        }
        else
        {
            // Create new progress record
            var lesson = await lessonRepository.GetByIdAsync(exam.LessonId, cancellationToken);
            if (lesson == null)
            {
                throw new KeyNotFoundException($"Lesson with ID {exam.LessonId} not found.");
            }

            var progress = Progress.Create(
                studentId: request.StudentId,
                lessonId: exam.LessonId,
                tenantId: tenantId
            );
            progress.RecordExamResult(result.Passed, result.Score);
            await progressRepository.AddAsync(progress, cancellationToken);
        }

        // Persist changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return result;
    }
}
