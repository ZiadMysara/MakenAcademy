using Maken.Application.Common.Interfaces;
using Maken.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Maken.Application.Commands.Courses;

/// <summary>
/// Handler for DeleteCourseCommand that soft deletes a course and cascades to related entities.
/// </summary>
public sealed class DeleteCourseCommandHandler : IRequestHandler<DeleteCourseCommand, DeleteCourseResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<DeleteCourseCommandHandler> _logger;

    public DeleteCourseCommandHandler(
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext,
        ILogger<DeleteCourseCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<DeleteCourseResult> Handle(DeleteCourseCommand request, CancellationToken cancellationToken)
    {
        // Get current tenant ID from context
        var tenantId = _tenantContext.TenantId 
            ?? throw new UnauthorizedAccessException("Tenant context is required.");

        _logger.LogInformation(
            "Soft deleting course {CourseId} and cascading to related entities for tenant {TenantId}",
            request.Id,
            tenantId);

        // Get repositories
        var courseRepository = _unitOfWork.Repository<Course>();
        var lessonRepository = _unitOfWork.Repository<Lesson>();
        var examRepository = _unitOfWork.Repository<Exam>();
        var questionRepository = _unitOfWork.Repository<Question>();
        var choiceRepository = _unitOfWork.Repository<Choice>();

        // Find course by ID
        var course = await courseRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Course with ID {request.Id} not found.");

        // Verify tenant ownership
        if (course.TenantId != tenantId)
        {
            throw new UnauthorizedAccessException("Cannot delete course from another tenant.");
        }

        // Get all lessons for this course
        var lessons = await lessonRepository.GetAllAsync(l => l.CourseId == request.Id, cancellationToken);

        // For each lesson, cascade delete exams, questions, and choices
        foreach (var lesson in lessons)
        {
            // Get all exams for this lesson
            var exams = await examRepository.GetAllAsync(e => e.LessonId == lesson.Id, cancellationToken);

            foreach (var exam in exams)
            {
                // Get all questions for this exam
                var questions = await questionRepository.GetAllAsync(q => q.ExamId == exam.Id, cancellationToken);

                foreach (var question in questions)
                {
                    // Get all choices for this question
                    var choices = await choiceRepository.GetAllAsync(c => c.QuestionId == question.Id, cancellationToken);

                    // Soft delete all choices
                    foreach (var choice in choices)
                    {
                        choice.SoftDelete();
                        choiceRepository.Update(choice);
                    }

                    // Soft delete question
                    question.SoftDelete();
                    questionRepository.Update(question);
                }

                // Soft delete exam
                exam.SoftDelete();
                examRepository.Update(exam);
            }

            // Soft delete lesson
            lesson.SoftDelete();
            lessonRepository.Update(lesson);
        }

        // Soft delete course
        course.SoftDelete();
        courseRepository.Update(course);

        // Persist all changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Successfully soft deleted course {CourseId} and {LessonCount} lessons for tenant {TenantId}",
            course.Id,
            lessons.Count(),
            tenantId);

        // Return result
        return new DeleteCourseResult(
            Id: course.Id,
            DeletedAt: course.DeletedAt ?? DateTime.UtcNow
        );
    }
}
