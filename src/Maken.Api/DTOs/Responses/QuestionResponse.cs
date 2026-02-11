namespace Maken.Api.DTOs.Responses;

/// <summary>
/// Response DTO for question data.
/// </summary>
public record QuestionResponse(
    Guid Id,
    Guid ExamId,
    string Text,
    int Order,
    List<ChoiceResponse> Choices
);
