using MediatR;

namespace Maken.Application.Commands.Exams;

/// <summary>
/// Command to update an existing exam.
/// </summary>
public sealed record UpdateExamCommand(
    Guid Id,
    string Title,
    int PassThreshold,
    List<UpdateQuestionDto> Questions
) : IRequest<UpdateExamResult>;

/// <summary>
/// DTO for updating a question within an exam.
/// </summary>
public sealed record UpdateQuestionDto(
    Guid? Id,
    string Text,
    int Order,
    List<UpdateChoiceDto> Choices
);

/// <summary>
/// DTO for updating a choice within a question.
/// </summary>
public sealed record UpdateChoiceDto(
    Guid? Id,
    string Text,
    bool IsCorrect
);

/// <summary>
/// Result of an exam update operation.
/// </summary>
public sealed record UpdateExamResult(
    Guid Id,
    Guid LessonId,
    Guid TenantId,
    string Title,
    int PassThreshold,
    DateTime UpdatedAt
);
