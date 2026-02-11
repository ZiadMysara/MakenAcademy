namespace Maken.Api.DTOs.Responses;

/// <summary>
/// Response DTO for exam data.
/// </summary>
public record ExamResponse(
    Guid Id,
    Guid LessonId,
    Guid TenantId,
    string Title,
    int PassThreshold,
    List<QuestionResponse> Questions,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
