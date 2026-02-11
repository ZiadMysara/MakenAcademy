using MediatR;

namespace Maken.Application.Queries.Analytics;

public record GetAnalyticsQuery : IRequest<AnalyticsResult>;

public record AnalyticsResult(
    int TotalEnrollments,
    decimal CompletionRate,
    decimal ExamPassRate
);
