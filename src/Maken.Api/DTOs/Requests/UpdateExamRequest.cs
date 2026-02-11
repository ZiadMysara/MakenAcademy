namespace Maken.Api.DTOs.Requests;

/// <summary>
/// Request DTO for updating an existing exam.
/// </summary>
public record UpdateExamRequest(
    string Title,
    int PassThreshold,
    List<UpdateQuestionRequest> Questions
);
