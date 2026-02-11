namespace Maken.Application.DTOs;

/// <summary>
/// DTO for exam result.
/// </summary>
public sealed record ExamResultDto(
    Guid ExamId,
    Guid StudentId,
    bool Passed,
    int Score,
    int TotalQuestions,
    int CorrectAnswers,
    DateTime AttemptedAt
);
