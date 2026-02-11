using MediatR;

namespace Maken.Application.Commands.Exams;

/// <summary>
/// Command to create a new exam with questions and choices.
/// </summary>
public sealed record CreateExamCommand(
    Guid LessonId,
    string Title,
    int PassThreshold,
    List<CreateQuestionDto> Questions
) : IRequest<CreateExamResult>;

/// <summary>
/// DTO for creating a question within an exam.
/// </summary>
public sealed record CreateQuestionDto(
    string Text,
    int Order,
    List<CreateChoiceDto> Choices
);

/// <summary>
/// DTO for creating a choice within a question.
/// </summary>
public sealed record CreateChoiceDto(
    string Text,
    bool IsCorrect
);

/// <summary>
/// Result of an exam creation operation.
/// </summary>
public sealed record CreateExamResult(
    Guid Id,
    Guid LessonId,
    Guid TenantId,
    string Title,
    int PassThreshold,
    DateTime CreatedAt
);
