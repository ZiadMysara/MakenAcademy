using Maken.Application.Common.Interfaces;

namespace Maken.Infrastructure.Interfaces;

/// <summary>
/// Repository interface for Exam entity operations.
/// Extends IRepository with exam-specific query methods.
/// </summary>
public interface IExamRepository : IRepository<object> // Will be replaced with Exam entity
{
    // Exam-specific methods will be added when Exam entity is created
    // - Task<Exam?> GetByIdWithQuestionsAsync(Guid id, CancellationToken cancellationToken);
    // - Task<Exam?> GetByLessonIdAsync(Guid lessonId, CancellationToken cancellationToken);
}
