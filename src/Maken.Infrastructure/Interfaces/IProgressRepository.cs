using Maken.Application.Common.Interfaces;

namespace Maken.Infrastructure.Interfaces;

/// <summary>
/// Repository interface for Progress entity operations.
/// Extends IRepository with progress-specific query methods.
/// </summary>
public interface IProgressRepository : IRepository<object> // Will be replaced with Progress entity
{
    // Progress-specific methods will be added when Progress entity is created
    // - Task<Progress?> GetByStudentAndLessonAsync(Guid studentId, Guid lessonId, CancellationToken cancellationToken);
    // - Task<List<Progress>> GetByStudentAndCourseAsync(Guid studentId, Guid courseId, CancellationToken cancellationToken);
}
