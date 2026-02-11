namespace Maken.Api.DTOs.Requests;

/// <summary>
/// Request DTO for submitting an exam with student answers.
/// </summary>
public record SubmitExamRequest(
    List<SubmitAnswerRequest> Answers
);

/// <summary>
/// Request DTO for a student's answer to a question.
/// </summary>
public record SubmitAnswerRequest(
    Guid QuestionId,
    Guid SelectedChoiceId
);
