using Maken.Application.Common.Interfaces;
using Maken.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Maken.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for Exam entity with specialized query methods.
/// </summary>
public class ExamRepository : Repository<Exam>, IExamRepository
{
    private readonly MakenDbContext _context;

    public ExamRepository(MakenDbContext context) : base(context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets an exam by ID with all questions and choices loaded.
    /// </summary>
    public async Task<Exam?> GetByIdWithQuestionsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Exams
            .Include(e => e.Questions)
            .ThenInclude(q => q.Choices)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    /// <summary>
    /// Gets an exam by lesson ID.
    /// </summary>
    public async Task<Exam?> GetByLessonIdAsync(Guid lessonId, CancellationToken cancellationToken = default)
    {
        return await _context.Exams
            .FirstOrDefaultAsync(e => e.LessonId == lessonId, cancellationToken);
    }

    /// <summary>
    /// Gets all questions for an exam.
    /// </summary>
    public async Task<List<Question>> GetQuestionsByExamIdAsync(Guid examId, CancellationToken cancellationToken = default)
    {
        return await _context.Questions
            .Where(q => q.ExamId == examId)
            .OrderBy(q => q.Order)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets all choices for a question.
    /// </summary>
    public async Task<List<Choice>> GetChoicesByQuestionIdAsync(Guid questionId, CancellationToken cancellationToken = default)
    {
        return await _context.Choices
            .Where(c => c.QuestionId == questionId)
            .ToListAsync(cancellationToken);
    }
}
