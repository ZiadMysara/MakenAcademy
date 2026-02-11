using Maken.Application.Common.Interfaces;
using Maken.Application.DTOs;
using Maken.Domain.Entities;
using MediatR;

namespace Maken.Application.Queries.Enrollments;

/// <summary>
/// Handler for getting all enrollments for a specific course.
/// </summary>
public class GetEnrollmentsQueryHandler : IRequestHandler<GetEnrollmentsQuery, List<EnrollmentDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;

    public GetEnrollmentsQueryHandler(
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext)
    {
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
    }

    public async Task<List<EnrollmentDto>> Handle(GetEnrollmentsQuery request, CancellationToken cancellationToken)
    {
        // Get tenant ID from context
        var tenantId = _tenantContext.TenantId 
            ?? throw new UnauthorizedAccessException("Tenant context is required.");

        // Verify course exists and belongs to tenant
        var courseRepository = _unitOfWork.Repository<Course>();
        var course = await courseRepository.GetByIdAsync(request.CourseId, cancellationToken);
        
        if (course == null)
        {
            throw new KeyNotFoundException($"Course with ID '{request.CourseId}' not found.");
        }

        if (course.TenantId != tenantId)
        {
            throw new UnauthorizedAccessException("You do not have permission to view enrollments for this course.");
        }

        // Get enrollments with student information
        var enrollmentRepository = _unitOfWork.Repository<Enrollment>();
        var enrollments = await enrollmentRepository.GetAllAsync(
            e => e.CourseId == request.CourseId && e.TenantId == tenantId,
            cancellationToken);

        // Get student information for each enrollment
        var userRepository = _unitOfWork.Repository<User>();
        var result = new List<EnrollmentDto>();

        foreach (var enrollment in enrollments.OrderBy(e => e.EnrolledAt))
        {
            var student = await userRepository.GetByIdAsync(enrollment.StudentId, cancellationToken);
            if (student != null)
            {
                result.Add(new EnrollmentDto(
                    Id: enrollment.Id,
                    StudentId: enrollment.StudentId,
                    CourseId: enrollment.CourseId,
                    TenantId: enrollment.TenantId,
                    EnrolledAt: enrollment.EnrolledAt,
                    CompletedAt: enrollment.CompletedAt,
                    StudentFirstName: student.FirstName,
                    StudentLastName: student.LastName,
                    StudentEmail: student.Email
                ));
            }
        }

        return result;
    }
}
