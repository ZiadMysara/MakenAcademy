using Maken.Application.Common.Interfaces;
using Maken.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Maken.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for Enrollment entity operations.
/// </summary>
public class EnrollmentRepository : Repository<Enrollment>, IEnrollmentRepository
{
    public EnrollmentRepository(MakenDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Gets an enrollment by student ID and course ID.
    /// </summary>
    public async Task<Enrollment?> GetByStudentAndCourseAsync(Guid studentId, Guid courseId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(e => e.StudentId == studentId && e.CourseId == courseId, cancellationToken);
    }

    /// <summary>
    /// Gets all enrollments for a specific student.
    /// </summary>
    public async Task<IEnumerable<Enrollment>> GetByStudentIdAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(e => e.Course)
            .Where(e => e.StudentId == studentId)
            .OrderBy(e => e.EnrolledAt)
            .ToListAsync(cancellationToken);
    }
}
