namespace Maken.Api.DTOs.Responses;

/// <summary>
/// Response DTO for exam result.
/// </summary>
public record ExamResultResponse(
    Guid ExamId,
    Guid StudentId,
    bool Passed,
    int Score,
    int TotalQuestions,
    int CorrectAnswers,
    DateTime AttemptedAt
);
