namespace Maken.Application.DTOs;

/// <summary>
/// Data transfer object for student progress information.
/// </summary>
public sealed record ProgressDto(
    Guid Id,
    Guid StudentId,
    Guid LessonId,
    DateTime? CompletedAt,
    bool? ExamPassed,
    int? ExamScore,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
