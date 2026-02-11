using Maken.Application.Common.Interfaces;
using Maken.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Maken.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for Progress entity operations.
/// </summary>
public class ProgressRepository : Repository<Progress>, IProgressRepository
{
    private readonly MakenDbContext _context;

    public ProgressRepository(MakenDbContext context) : base(context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Progress>> GetByStudentAndCourseAsync(Guid studentId, Guid courseId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Progress>()
            .Include(p => p.Lesson)
            .Where(p => p.StudentId == studentId && p.Lesson!.CourseId == courseId)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Progress?> GetByStudentAndLessonAsync(Guid studentId, Guid lessonId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Progress>()
            .FirstOrDefaultAsync(p => p.StudentId == studentId && p.LessonId == lessonId, cancellationToken);
    }
}
