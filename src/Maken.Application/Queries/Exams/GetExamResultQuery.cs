using Maken.Application.DTOs;
using MediatR;

namespace Maken.Application.Queries.Exams;

/// <summary>
/// Query to get exam result for a student.
/// </summary>
public sealed record GetExamResultQuery(
    Guid ExamId,
    Guid StudentId
) : IRequest<ExamResultDto?>;
