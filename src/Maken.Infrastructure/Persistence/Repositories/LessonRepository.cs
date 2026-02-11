using Maken.Domain.Entities;
using Maken.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Maken.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for Lesson entity.
/// </summary>
public sealed class LessonRepository : Repository<Lesson>, ILessonRepository
{
    private readonly MakenDbContext _context;

    public LessonRepository(MakenDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<List<Lesson>> GetByCourseIdAsync(Guid courseId, CancellationToken cancellationToken = default)
    {
        return await _context.Lessons
            .Where(l => l.CourseId == courseId)
            .OrderBy(l => l.Order)
            .ToListAsync(cancellationToken);
    }

    public async Task<Lesson?> GetByIdWithExamAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Lessons
            .Include(l => l.Exam)
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
    }
}
