namespace Maken.Api.DTOs.Requests;

/// <summary>
/// Request DTO for creating a new exam.
/// </summary>
public record CreateExamRequest(
    Guid LessonId,
    string Title,
    int PassThreshold,
    List<CreateQuestionRequest> Questions
);
