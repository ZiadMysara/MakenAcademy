using MediatR;
using Maken.Application.Common.Interfaces;
using Maken.Application.DTOs;
using Maken.Domain.Services;

namespace Maken.Application.Queries.Courses;

/// <summary>
/// Handler for retrieving courses for a student with locked/unlocked status based on prerequisites and progression
/// </summary>
public class GetStudentCoursesQueryHandler : IRequestHandler<GetStudentCoursesQuery, IEnumerable<CourseDto>>
{
    private readonly ICourseRepository _courseRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IProgressRepository _progressRepository;
    private readonly ProgressionService _progressionService;

    public GetStudentCoursesQueryHandler(
        ICourseRepository courseRepository,
        IEnrollmentRepository enrollmentRepository,
        IProgressRepository progressRepository,
        ProgressionService progressionService)
    {
        _courseRepository = courseRepository;
        _enrollmentRepository = enrollmentRepository;
        _progressRepository = progressRepository;
        _progressionService = progressionService;
    }

    public async Task<IEnumerable<CourseDto>> Handle(GetStudentCoursesQuery request, CancellationToken cancellationToken)
    {
        // Get all published courses
        var courses = await _courseRepository.GetPublishedCoursesAsync(cancellationToken);

        // Get student enrollments
        var enrollments = await _enrollmentRepository.GetByStudentIdAsync(request.StudentId, cancellationToken);
        var enrolledCourseIds = enrollments.Select(e => e.CourseId).ToHashSet();

        var courseDtos = new List<CourseDto>();

        foreach (var course in courses)
        {
            // Check if student is enrolled
            var isEnrolled = enrolledCourseIds.Contains(course.Id);

            // Calculate locked/unlocked status
            bool isLocked = false;
            int completionPercentage = 0;

            if (isEnrolled)
            {
                // Check prerequisites
                if (course.PrerequisiteCourseIds.Any())
                {
                    var allPrerequisitesCompleted = await AreAllPrerequisitesCompletedAsync(
                        course.PrerequisiteCourseIds,
                        request.StudentId,
                        cancellationToken);

                    isLocked = !_progressionService.IsCourseUnlocked(
                        hasPrerequisites: true,
                        allPrerequisitesCompleted: allPrerequisitesCompleted);
                }

                // Calculate completion percentage
                var progress = await _progressRepository.GetByStudentAndCourseAsync(
                    request.StudentId,
                    course.Id,
                    cancellationToken);

                var totalLessons = course.Lessons.Count;
                var completedLessons = progress.Count(p => p.CompletedAt.HasValue && (p.ExamPassed ?? true));

                completionPercentage = (int)_progressionService.CalculateCourseCompletion(totalLessons, completedLessons);
            }
            else
            {
                // Not enrolled - course is locked
                isLocked = true;
            }

            courseDtos.Add(new CourseDto(
                course.Id,
                course.TenantId,
                course.Name,
                course.Description,
                course.Status.ToString(),
                course.FreeFlowMode,
                course.PrerequisiteCourseIds,
                course.CreatedAt,
                course.UpdatedAt,
                IsLocked: isLocked,
                CompletionPercentage: completionPercentage
            ));
        }

        // Apply pagination
        return courseDtos
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize);
    }

    private async Task<bool> AreAllPrerequisitesCompletedAsync(
        List<Guid> prerequisiteCourseIds,
        Guid studentId,
        CancellationToken cancellationToken)
    {
        foreach (var prerequisiteCourseId in prerequisiteCourseIds)
        {
            var course = await _courseRepository.GetByIdWithLessonsAsync(prerequisiteCourseId, cancellationToken);
            if (course == null)
                return false;

            var progress = await _progressRepository.GetByStudentAndCourseAsync(
                studentId,
                prerequisiteCourseId,
                cancellationToken);

            var totalLessons = course.Lessons.Count;
            var completedLessons = progress.Count(p => p.CompletedAt.HasValue && (p.ExamPassed ?? true));

            if (completedLessons < totalLessons)
                return false;
        }

        return true;
    }
}
