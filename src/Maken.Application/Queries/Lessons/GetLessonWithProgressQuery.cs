using MediatR;
using Maken.Application.DTOs;

namespace Maken.Application.Queries.Lessons;

/// <summary>
/// Query to get a lesson with progress information for a student.
/// Includes locked/unlocked status based on progression rules.
/// </summary>
public record GetLessonWithProgressQuery(
    Guid StudentId,
    Guid CourseId,
    Guid LessonId
) : IRequest<LessonDto?>;
