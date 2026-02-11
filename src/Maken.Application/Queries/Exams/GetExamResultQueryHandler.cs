using Maken.Application.Common.Interfaces;
using Maken.Application.DTOs;
using Maken.Domain.Entities;
using MediatR;

namespace Maken.Application.Queries.Exams;

/// <summary>
/// Handler for GetExamResultQuery that retrieves exam result for a student.
/// </summary>
public sealed class GetExamResultQueryHandler : IRequestHandler<GetExamResultQuery, ExamResultDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;

    public GetExamResultQueryHandler(
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext)
    {
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
    }

    public async Task<ExamResultDto?> Handle(GetExamResultQuery request, CancellationToken cancellationToken)
    {
        // Get current tenant ID from context
        Guid tenantId = _tenantContext.TenantId 
            ?? throw new UnauthorizedAccessException("Tenant context is required.");

        // Get repositories
        IRepository<Exam> examRepository = _unitOfWork.Repository<Exam>();
        IRepository<Progress> progressRepository = _unitOfWork.Repository<Progress>();

        // Find exam
        Exam? exam = await examRepository.GetByIdAsync(request.ExamId, cancellationToken);
        if (exam == null)
        {
            return null;
        }

        // Verify tenant ownership
        if (exam.TenantId != tenantId)
        {
            throw new UnauthorizedAccessException("You do not have permission to access this exam.");
        }

        // Get progress record for this student and lesson
        var progress = (await progressRepository.GetAllAsync(
            p => p.StudentId == request.StudentId && p.LessonId == exam.LessonId,
            cancellationToken)).FirstOrDefault();

        if (progress == null || progress.ExamScore == null)
        {
            return null; // No exam attempt found
        }

        // Get question count for the exam
        IRepository<Question> questionRepository = _unitOfWork.Repository<Question>();
        var questions = await questionRepository.GetAllAsync(
            q => q.ExamId == exam.Id,
            cancellationToken);

        int totalQuestions = questions.Count;
        int correctAnswers = (int)Math.Round((double)progress.ExamScore.Value * totalQuestions / 100);

        return new ExamResultDto(
            ExamId: exam.Id,
            StudentId: request.StudentId,
            Passed: progress.ExamPassed ?? false,
            Score: progress.ExamScore.Value,
            TotalQuestions: totalQuestions,
            CorrectAnswers: correctAnswers,
            AttemptedAt: progress.CompletedAt ?? progress.CreatedAt
        );
    }
}
