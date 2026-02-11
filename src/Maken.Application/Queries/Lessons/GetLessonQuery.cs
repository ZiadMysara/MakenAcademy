using Maken.Domain.Entities;
using MediatR;

namespace Maken.Application.Queries.Lessons;

/// <summary>
/// Query to get a single lesson by ID.
/// </summary>
public sealed record GetLessonQuery(Guid Id) : IRequest<Lesson>;
