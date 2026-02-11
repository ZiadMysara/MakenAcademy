using Maken.Domain.Entities;

namespace Maken.Application.Common.Interfaces;

/// <summary>
/// Repository interface for Progress entity with specific query methods
/// </summary>
public interface IProgressRepository : IRepository<Progress>
{
    Task<IEnumerable<Progress>> GetByStudentAndCourseAsync(Guid studentId, Guid courseId, CancellationToken cancellationToken = default);
    Task<Progress?> GetByStudentAndLessonAsync(Guid studentId, Guid lessonId, CancellationToken cancellationToken = default);
}
