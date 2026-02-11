using Maken.Api.DTOs.Requests;
using Maken.Api.DTOs.Responses;
using Maken.Application.Commands.Lessons;
using Maken.Application.Queries.Lessons;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maken.Api.Controllers;

/// <summary>
/// Controller for lesson management operations.
/// Provides CRUD endpoints for lessons within courses with tenant isolation.
/// </summary>
[ApiController]
[Route("api/courses/{courseId}/lessons")]
[Authorize]
public sealed class LessonsController : ControllerBase
{
    private readonly IMediator _mediator;

    public LessonsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Gets all lessons for a course, ordered by Order field.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<LessonResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<LessonResponse>>> GetLessons(
        Guid courseId,
        CancellationToken cancellationToken)
    {
        try
        {
            var query = new GetLessonsQuery(courseId);
            var result = await _mediator.Send(query, cancellationToken);

            var lessons = result.Lessons.Select(l => new LessonResponse(
                Id: l.Id,
                CourseId: l.CourseId,
                Title: l.Title,
                Description: l.Description,
                ContentType: l.ContentType.ToString(),
                ContentUrl: l.ContentUrl,
                Order: l.Order,
                IsLocked: false, // Will be calculated based on student progress
                IsCompleted: false, // Will be calculated based on student progress
                CreatedAt: l.CreatedAt,
                UpdatedAt: l.UpdatedAt
            )).ToList();

            return Ok(lessons);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Gets a specific lesson by ID.
    /// </summary>
    [HttpGet("{lessonId}")]
    [ProducesResponseType(typeof(LessonResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LessonResponse>> GetLesson(
        Guid courseId,
        Guid lessonId,
        CancellationToken cancellationToken)
    {
        try
        {
            var query = new GetLessonQuery(lessonId);
            var lesson = await _mediator.Send(query, cancellationToken);

            // Verify lesson belongs to the specified course
            if (lesson.CourseId != courseId)
            {
                return NotFound(new { message = $"Lesson {lessonId} not found in course {courseId}" });
            }

            var response = new LessonResponse(
                Id: lesson.Id,
                CourseId: lesson.CourseId,
                Title: lesson.Title,
                Description: lesson.Description,
                ContentType: lesson.ContentType.ToString(),
                ContentUrl: lesson.ContentUrl,
                Order: lesson.Order,
                IsLocked: false, // Will be calculated based on student progress
                IsCompleted: false, // Will be calculated based on student progress
                CreatedAt: lesson.CreatedAt,
                UpdatedAt: lesson.UpdatedAt
            );

            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Creates a new lesson in a course.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "CompanyAdmin,Instructor")]
    [ProducesResponseType(typeof(LessonResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<LessonResponse>> CreateLesson(
        Guid courseId,
        [FromBody] CreateLessonRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new CreateLessonCommand(
                CourseId: courseId,
                Title: request.Title,
                Description: request.Description,
                ContentType: request.ContentType,
                ContentUrl: request.ContentUrl,
                Order: request.Order
            );

            var lesson = await _mediator.Send(command, cancellationToken);

            var response = new LessonResponse(
                Id: lesson.Id,
                CourseId: courseId,
                Title: lesson.Title,
                Description: lesson.Description,
                ContentType: lesson.ContentType,
                ContentUrl: lesson.ContentUrl,
                Order: lesson.Order,
                IsLocked: false,
                IsCompleted: false,
                CreatedAt: lesson.CreatedAt,
                UpdatedAt: null
            );

            return CreatedAtAction(
                nameof(GetLesson),
                new { courseId = courseId, lessonId = lesson.Id },
                response
            );
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Updates an existing lesson.
    /// </summary>
    [HttpPut("{lessonId}")]
    [Authorize(Roles = "CompanyAdmin,Instructor")]
    [ProducesResponseType(typeof(LessonResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<LessonResponse>> UpdateLesson(
        Guid courseId,
        Guid lessonId,
        [FromBody] UpdateLessonRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new UpdateLessonCommand(
                Id: lessonId,
                Title: request.Title,
                Description: request.Description,
                ContentType: request.ContentType,
                ContentUrl: request.ContentUrl,
                Order: request.Order
            );

            var lesson = await _mediator.Send(command, cancellationToken);

            var response = new LessonResponse(
                Id: lesson.Id,
                CourseId: courseId,
                Title: lesson.Title,
                Description: lesson.Description,
                ContentType: lesson.ContentType,
                ContentUrl: lesson.ContentUrl,
                Order: lesson.Order,
                IsLocked: false,
                IsCompleted: false,
                CreatedAt: DateTime.UtcNow, // Not available in UpdateLessonResult
                UpdatedAt: lesson.UpdatedAt
            );

            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Soft deletes a lesson and cascades to related entities.
    /// </summary>
    [HttpDelete("{lessonId}")]
    [Authorize(Roles = "CompanyAdmin,Instructor")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteLesson(
        Guid courseId,
        Guid lessonId,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new DeleteLessonCommand(lessonId);
            await _mediator.Send(command, cancellationToken);

            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
