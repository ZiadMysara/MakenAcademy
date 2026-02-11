using Maken.Domain.ValueObjects;
using MediatR;

namespace Maken.Application.Commands.Exams;

/// <summary>
/// Command to submit an exam with student answers.
/// </summary>
public sealed record SubmitExamCommand(
    Guid ExamId,
    Guid StudentId,
    List<SubmitAnswerDto> Answers
) : IRequest<ExamResult>;

/// <summary>
/// DTO for a student's answer to a question.
/// </summary>
public sealed record SubmitAnswerDto(
    Guid QuestionId,
    Guid SelectedChoiceId
);
