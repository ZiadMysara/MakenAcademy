using Maken.Application.Common.Interfaces;
using Maken.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Maken.Application.Commands.Students;

/// <summary>
/// Handler for enrolling a student in a course.
/// </summary>
public class EnrollStudentCommandHandler : IRequestHandler<EnrollStudentCommand, EnrollStudentResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<EnrollStudentCommandHandler> _logger;

    public EnrollStudentCommandHandler(
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext,
        ILogger<EnrollStudentCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<EnrollStudentResult> Handle(EnrollStudentCommand request, CancellationToken cancellationToken)
    {
        // Get tenant ID from context
        var tenantId = _tenantContext.TenantId 
            ?? throw new UnauthorizedAccessException("Tenant context is required.");

        _logger.LogInformation("Enrolling student {StudentId} in course {CourseId} for tenant {TenantId}", 
            request.StudentId, request.CourseId, tenantId);

        // Verify course exists and belongs to tenant
        var courseRepository = _unitOfWork.Repository<Course>();
        var course = await courseRepository.GetByIdAsync(request.CourseId, cancellationToken);
        
        if (course == null)
        {
            _logger.LogWarning("Course {CourseId} not found", request.CourseId);
            throw new KeyNotFoundException($"Course with ID '{request.CourseId}' not found.");
        }

        if (course.TenantId != tenantId)
        {
            _logger.LogWarning("Unauthorized attempt to enroll in course {CourseId} from tenant {TenantId}", 
                request.CourseId, tenantId);
            throw new UnauthorizedAccessException("You do not have permission to enroll students in this course.");
        }

        // Verify student exists and belongs to tenant
        var userRepository = _unitOfWork.Repository<User>();
        var student = await userRepository.GetByIdAsync(request.StudentId, cancellationToken);
        
        if (student == null)
        {
            _logger.LogWarning("Student {StudentId} not found", request.StudentId);
            throw new KeyNotFoundException($"Student with ID '{request.StudentId}' not found.");
        }

        if (student.TenantId != tenantId)
        {
            _logger.LogWarning("Unauthorized attempt to enroll student {StudentId} from tenant {TenantId}", 
                request.StudentId, tenantId);
            throw new UnauthorizedAccessException("You do not have permission to enroll this student.");
        }

        // Check if enrollment already exists (unique constraint)
        var enrollmentRepository = _unitOfWork.Repository<Enrollment>();
        var existingEnrollments = await enrollmentRepository.GetAllAsync(
            e => e.StudentId == request.StudentId && e.CourseId == request.CourseId,
            cancellationToken);

        if (existingEnrollments.Any())
        {
            _logger.LogWarning("Student {StudentId} is already enrolled in course {CourseId}", 
                request.StudentId, request.CourseId);
            throw new InvalidOperationException($"Student is already enrolled in this course.");
        }

        // Create enrollment
        var enrollment = new Enrollment
        {
            StudentId = request.StudentId,
            CourseId = request.CourseId,
            TenantId = tenantId,
            EnrolledAt = DateTime.UtcNow
        };

        await enrollmentRepository.AddAsync(enrollment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully enrolled student {StudentId} in course {CourseId}", 
            request.StudentId, request.CourseId);

        return new EnrollStudentResult(
            Id: enrollment.Id,
            StudentId: enrollment.StudentId,
            CourseId: enrollment.CourseId,
            TenantId: enrollment.TenantId,
            EnrolledAt: enrollment.EnrolledAt
        );
    }
}
