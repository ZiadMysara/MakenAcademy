namespace Maken.Api.DTOs.Requests;

/// <summary>
/// Request DTO for updating a choice within a question.
/// </summary>
public record UpdateChoiceRequest(
    Guid? Id,
    string Text,
    bool IsCorrect
);
