using Maken.Domain.Entities;

namespace Maken.Application.Common.Interfaces;

/// <summary>
/// Repository interface for Course entity with specific query methods
/// </summary>
public interface ICourseRepository : IRepository<Course>
{
    Task<IEnumerable<Course>> GetPublishedCoursesAsync(CancellationToken cancellationToken = default);
    Task<Course?> GetByIdWithLessonsAsync(Guid id, CancellationToken cancellationToken = default);
}
