namespace Maken.Api.DTOs.Responses;

/// <summary>
/// Response DTO for choice data.
/// </summary>
public record ChoiceResponse(
    Guid Id,
    Guid QuestionId,
    string Text,
    bool? IsCorrect
);
