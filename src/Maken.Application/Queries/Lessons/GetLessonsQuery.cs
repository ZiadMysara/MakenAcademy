using Maken.Domain.Entities;
using MediatR;

namespace Maken.Application.Queries.Lessons;

/// <summary>
/// Query to get all lessons for a course, ordered by Order field.
/// </summary>
public sealed record GetLessonsQuery(Guid CourseId) : IRequest<GetLessonsQueryResult>;

/// <summary>
/// Result of GetLessonsQuery.
/// </summary>
public sealed record GetLessonsQueryResult(
    List<Lesson> Lessons
);
