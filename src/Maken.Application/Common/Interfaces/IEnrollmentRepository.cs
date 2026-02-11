using Maken.Domain.Entities;

namespace Maken.Application.Common.Interfaces;

/// <summary>
/// Repository interface for Enrollment entity with specific query methods
/// </summary>
public interface IEnrollmentRepository : IRepository<Enrollment>
{
    Task<IEnumerable<Enrollment>> GetByStudentIdAsync(Guid studentId, CancellationToken cancellationToken = default);
    Task<Enrollment?> GetByStudentAndCourseAsync(Guid studentId, Guid courseId, CancellationToken cancellationToken = default);
}
