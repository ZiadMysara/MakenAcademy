using Maken.Application.Common.Interfaces;
using MediatR;

namespace Maken.Application.Queries.Analytics;

public class GetAnalyticsQueryHandler : IRequestHandler<GetAnalyticsQuery, AnalyticsResult>
{
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IProgressRepository _progressRepository;

    public GetAnalyticsQueryHandler(
        IEnrollmentRepository enrollmentRepository,
        IProgressRepository progressRepository)
    {
        _enrollmentRepository = enrollmentRepository;
        _progressRepository = progressRepository;
    }

    public async Task<AnalyticsResult> Handle(GetAnalyticsQuery request, CancellationToken cancellationToken)
    {
        // Get all enrollments for the tenant (automatically filtered by tenant context)
        var enrollments = await _enrollmentRepository.GetAllAsync(cancellationToken: cancellationToken);
        var totalEnrollments = enrollments.Count;

        // Get all progress records for the tenant
        var progressRecords = await _progressRepository.GetAllAsync(cancellationToken: cancellationToken);

        // Calculate completion rate (enrollments with CompletedAt set)
        var completedEnrollments = enrollments.Count(e => e.CompletedAt.HasValue);
        var completionRate = totalEnrollments > 0 
            ? (decimal)completedEnrollments / totalEnrollments * 100 
            : 0;

        // Calculate exam pass rate (progress records with ExamPassed = true)
        var examAttempts = progressRecords.Count(p => p.ExamScore.HasValue);
        var passedExams = progressRecords.Count(p => p.ExamPassed == true);
        var examPassRate = examAttempts > 0 
            ? (decimal)passedExams / examAttempts * 100 
            : 0;

        return new AnalyticsResult(
            TotalEnrollments: totalEnrollments,
            CompletionRate: Math.Round(completionRate, 2),
            ExamPassRate: Math.Round(examPassRate, 2)
        );
    }
}
