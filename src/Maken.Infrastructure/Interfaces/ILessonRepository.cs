using Maken.Application.Common.Interfaces;
using Maken.Domain.Entities;

namespace Maken.Infrastructure.Interfaces;

/// <summary>
/// Repository interface for Lesson entity with specialized query methods.
/// </summary>
public interface ILessonRepository : IRepository<Lesson>
{
    /// <summary>
    /// Gets all lessons for a specific course, ordered by Order field.
    /// </summary>
    Task<List<Lesson>> GetByCourseIdAsync(Guid courseId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a lesson by ID with its associated exam loaded.
    /// </summary>
    Task<Lesson?> GetByIdWithExamAsync(Guid id, CancellationToken cancellationToken = default);
}
