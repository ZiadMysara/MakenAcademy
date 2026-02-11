namespace Maken.Api.DTOs.Requests;

/// <summary>
/// Request DTO for creating a choice within a question.
/// </summary>
public record CreateChoiceRequest(
    string Text,
    bool IsCorrect
);
