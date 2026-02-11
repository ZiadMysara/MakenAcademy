using MediatR;
using Maken.Application.Common.Interfaces;
using Maken.Application.DTOs;
using Maken.Domain.Services;

namespace Maken.Application.Queries.Lessons;

/// <summary>
/// Handler for retrieving a lesson with progress information.
/// Checks if the lesson is locked based on progression rules.
/// </summary>
public class GetLessonWithProgressQueryHandler : IRequestHandler<GetLessonWithProgressQuery, LessonDto?>
{
    private readonly IRepository<Domain.Entities.Lesson> _lessonRepository;
    private readonly ICourseRepository _courseRepository;
    private readonly IProgressRepository _progressRepository;
    private readonly ProgressionService _progressionService;

    public GetLessonWithProgressQueryHandler(
        IRepository<Domain.Entities.Lesson> lessonRepository,
        ICourseRepository courseRepository,
        IProgressRepository progressRepository,
        ProgressionService progressionService)
    {
        _lessonRepository = lessonRepository;
        _courseRepository = courseRepository;
        _progressRepository = progressRepository;
        _progressionService = progressionService;
    }

    public async Task<LessonDto?> Handle(GetLessonWithProgressQuery request, CancellationToken cancellationToken)
    {
        // Get the lesson
        var lesson = await _lessonRepository.GetByIdAsync(request.LessonId, cancellationToken);
        if (lesson == null)
            return null;

        // Get the course to check free flow mode
        var course = await _courseRepository.GetByIdAsync(request.CourseId, cancellationToken);
        if (course == null)
            return null;

        // Get student's progress for this lesson
        var progress = await _progressRepository.GetByStudentAndLessonAsync(
            request.StudentId,
            request.LessonId,
            cancellationToken);

        // Determine if lesson is locked
        bool isLocked = false;
        
        // If Free Flow mode is enabled, all lessons are unlocked
        if (!course.FreeFlowMode && lesson.Order > 1)
        {
            // Get all progress for the student in this course
            var allProgress = await _progressRepository.GetByStudentAndCourseAsync(
                request.StudentId,
                request.CourseId,
                cancellationToken);

            // We need to find the previous lesson (Order = current - 1)
            // Since we don't have the Lesson navigation property loaded, we need to query for it
            // For now, we'll check if there's any progress record for a lesson with Order = current - 1
            // This requires getting all lessons for the course
            var allLessons = await _courseRepository.GetByIdWithLessonsAsync(request.CourseId, cancellationToken);
            var previousLesson = allLessons?.Lessons?.FirstOrDefault(l => l.Order == lesson.Order - 1);

            if (previousLesson != null)
            {
                var previousLessonProgress = allProgress.FirstOrDefault(p => p.LessonId == previousLesson.Id);

                bool previousLessonCompleted = previousLessonProgress?.CompletedAt.HasValue ?? false;
                bool? previousLessonExamPassed = previousLessonProgress?.ExamPassed;

                isLocked = !_progressionService.IsLessonUnlocked(
                    lesson.Order,
                    course.FreeFlowMode,
                    previousLessonCompleted,
                    previousLessonExamPassed);
            }
            else
            {
                // If there's no previous lesson, something is wrong - lock the lesson
                isLocked = true;
            }
        }

        return new LessonDto(
            lesson.Id,
            lesson.CourseId,
            lesson.Title,
            lesson.Description,
            lesson.ContentType.ToString(),
            lesson.ContentUrl,
            lesson.Order,
            lesson.Exam != null,
            lesson.CreatedAt,
            lesson.UpdatedAt,
            IsLocked: isLocked,
            IsCompleted: progress?.IsFullyCompleted() ?? false
        );
    }
}
