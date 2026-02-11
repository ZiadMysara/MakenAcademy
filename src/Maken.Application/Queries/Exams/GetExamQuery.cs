using Maken.Application.DTOs;
using MediatR;

namespace Maken.Application.Queries.Exams;

/// <summary>
/// Query to get an exam by ID.
/// </summary>
public sealed record GetExamQuery(Guid Id, bool IncludeAnswers = false) : IRequest<ExamDto?>;
