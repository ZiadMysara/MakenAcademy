namespace Maken.Api.DTOs.Requests;

/// <summary>
/// Request DTO for creating a question within an exam.
/// </summary>
public record CreateQuestionRequest(
    string Text,
    int Order,
    List<CreateChoiceRequest> Choices
);
