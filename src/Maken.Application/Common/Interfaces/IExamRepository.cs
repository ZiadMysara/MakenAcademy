using Maken.Domain.Entities;

namespace Maken.Application.Common.Interfaces;

/// <summary>
/// Repository interface for Exam entity with specialized query methods.
/// </summary>
public interface IExamRepository : IRepository<Exam>
{
    /// <summary>
    /// Gets an exam by ID with all questions and choices loaded.
    /// </summary>
    /// <param name="id">The exam ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The exam with questions and choices if found, otherwise null.</returns>
    Task<Exam?> GetByIdWithQuestionsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an exam by lesson ID.
    /// </summary>
    /// <param name="lessonId">The lesson ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The exam if found, otherwise null.</returns>
    Task<Exam?> GetByLessonIdAsync(Guid lessonId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all questions for an exam.
    /// </summary>
    /// <param name="examId">The exam ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of questions ordered by Order property.</returns>
    Task<List<Question>> GetQuestionsByExamIdAsync(Guid examId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all choices for a question.
    /// </summary>
    /// <param name="questionId">The question ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of choices.</returns>
    Task<List<Choice>> GetChoicesByQuestionIdAsync(Guid questionId, CancellationToken cancellationToken = default);
}
