namespace Maken.Api.DTOs.Requests;

/// <summary>
/// Request DTO for updating a question within an exam.
/// </summary>
public record UpdateQuestionRequest(
    Guid? Id,
    string Text,
    int Order,
    List<UpdateChoiceRequest> Choices
);
