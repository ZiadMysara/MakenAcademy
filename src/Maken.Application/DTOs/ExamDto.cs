namespace Maken.Application.DTOs;

/// <summary>
/// DTO for exam data.
/// </summary>
public sealed record ExamDto(
    Guid Id,
    Guid LessonId,
    Guid TenantId,
    string Title,
    int PassThreshold,
    List<QuestionDto> Questions,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

/// <summary>
/// DTO for question data.
/// </summary>
public sealed record QuestionDto(
    Guid Id,
    Guid ExamId,
    string Text,
    int Order,
    List<ChoiceDto> Choices
);

/// <summary>
/// DTO for choice data.
/// </summary>
public sealed record ChoiceDto(
    Guid Id,
    Guid QuestionId,
    string Text,
    bool? IsCorrect
);
