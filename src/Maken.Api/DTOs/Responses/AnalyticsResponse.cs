namespace Maken.Api.DTOs.Responses;

public record AnalyticsResponse(
    int TotalEnrollments,
    decimal CompletionRate,
    decimal ExamPassRate
);
