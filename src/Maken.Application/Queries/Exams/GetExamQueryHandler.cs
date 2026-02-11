using Maken.Application.Common.Interfaces;
using Maken.Application.DTOs;
using Maken.Domain.Entities;
using MediatR;

namespace Maken.Application.Queries.Exams;

/// <summary>
/// Handler for GetExamQuery that retrieves an exam by ID.
/// Hides correct answers for students unless IncludeAnswers is true.
/// </summary>
public sealed class GetExamQueryHandler : IRequestHandler<GetExamQuery, ExamDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;

    public GetExamQueryHandler(
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext)
    {
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
    }

    public async Task<ExamDto?> Handle(GetExamQuery request, CancellationToken cancellationToken)
    {
        // Get current tenant ID from context
        Guid tenantId = _tenantContext.TenantId 
            ?? throw new UnauthorizedAccessException("Tenant context is required.");

        // Get repositories
        IRepository<Exam> examRepository = _unitOfWork.Repository<Exam>();
        IRepository<Question> questionRepository = _unitOfWork.Repository<Question>();
        IRepository<Choice> choiceRepository = _unitOfWork.Repository<Choice>();

        // Find exam
        Exam? exam = await examRepository.GetByIdAsync(request.Id, cancellationToken);
        if (exam == null)
        {
            return null;
        }

        // Verify tenant ownership
        if (exam.TenantId != tenantId)
        {
            throw new UnauthorizedAccessException("You do not have permission to access this exam.");
        }

        // Get questions for this exam, ordered by Order property
        var questions = await questionRepository.GetAllAsync(
            q => q.ExamId == exam.Id,
            cancellationToken);
        
        var orderedQuestions = questions.OrderBy(q => q.Order).ToList();

        // Build question DTOs
        var questionDtos = new List<QuestionDto>();
        foreach (var question in orderedQuestions)
        {
            // Get choices for this question
            var choices = await choiceRepository.GetAllAsync(
                c => c.QuestionId == question.Id,
                cancellationToken);

            // Map choices to DTOs, hiding correct answers if needed
            var choiceDtos = choices.Select(c => new ChoiceDto(
                Id: c.Id,
                QuestionId: c.QuestionId,
                Text: c.Text,
                IsCorrect: request.IncludeAnswers ? c.IsCorrect : null
            )).ToList();

            questionDtos.Add(new QuestionDto(
                Id: question.Id,
                ExamId: question.ExamId,
                Text: question.Text,
                Order: question.Order,
                Choices: choiceDtos
            ));
        }

        // Return exam DTO
        return new ExamDto(
            Id: exam.Id,
            LessonId: exam.LessonId,
            TenantId: exam.TenantId,
            Title: exam.Title,
            PassThreshold: exam.PassThreshold,
            Questions: questionDtos,
            CreatedAt: exam.CreatedAt,
            UpdatedAt: exam.UpdatedAt
        );
    }
}
