using Maken.Application.Common.Interfaces;
using Maken.Domain.Entities;
using Maken.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Maken.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for Course entity operations.
/// Provides course-specific query methods with eager loading support.
/// </summary>
public class CourseRepository : Repository<Course>, ICourseRepository
{
    private readonly MakenDbContext _context;

    public CourseRepository(MakenDbContext context) : base(context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Course?> GetByIdWithLessonsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Load course with lessons
        var course = await _context.Set<Course>()
            .Include(c => c.Lessons)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        // EF Core doesn't support OrderBy in Include, so we need to manually sort the collection
        // This is a known limitation: https://github.com/dotnet/efcore/issues/18022
        if (course != null && course.Lessons.Any())
        {
            var orderedLessons = course.Lessons.OrderBy(l => l.Order).ToList();
            course.Lessons.Clear();
            foreach (var lesson in orderedLessons)
            {
                course.Lessons.Add(lesson);
            }
        }

        return course;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Course>> GetPublishedCoursesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<Course>()
            .Include(c => c.Lessons)
            .Where(c => c.Status == CourseStatus.Published)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }
}
