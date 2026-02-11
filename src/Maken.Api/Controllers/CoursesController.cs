using Maken.Api.DTOs.Requests;
using Maken.Api.DTOs.Responses;
using Maken.Application.Commands.Courses;
using Maken.Application.Queries.Courses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maken.Api.Controllers;

/// <summary>
/// Controller for course management operations.
/// Provides CRUD endpoints for courses with tenant isolation.
/// </summary>
[ApiController]
[Route("api/courses")]
[Authorize]
public sealed class CoursesController : ControllerBase
{
    private readonly IMediator _mediator;

    public CoursesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Gets a paginated list of courses for the current tenant.
    /// </summary>
    /// <param name="pageNumber">Page number (default: 1).</param>
    /// <param name="pageSize">Page size (default: 20).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated list of courses.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(CourseListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CourseListResponse>> GetCourses(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetCoursesQuery(pageNumber, pageSize);
        var result = await _mediator.Send(query, cancellationToken);

        var courses = result.Courses.Select(c => new CourseResponse(
            Id: c.Id,
            Name: c.Name,
            Description: c.Description,
            Status: c.Status.ToString(),
            FreeFlowMode: c.FreeFlowMode,
            PrerequisiteCourseIds: c.PrerequisiteCourseIds,
            IsLocked: false, // Will be calculated based on student context
            CompletionPercentage: 0, // Will be calculated based on student progress
            CreatedAt: c.CreatedAt,
            UpdatedAt: c.UpdatedAt
        )).ToList();

        var response = new CourseListResponse(
            Courses: courses,
            TotalCount: result.TotalCount,
            PageNumber: pageNumber,
            PageSize: pageSize
        );

        return Ok(response);
    }

    /// <summary>
    /// Gets a specific course by ID.
    /// </summary>
    /// <param name="id">Course ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Course details.</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(CourseResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CourseResponse>> GetCourse(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var query = new GetCourseQuery(id);
            var course = await _mediator.Send(query, cancellationToken);

            if (course == null)
            {
                return NotFound(new { message = $"Course with ID '{id}' not found" });
            }

            var response = new CourseResponse(
                Id: course.Id,
                Name: course.Name,
                Description: course.Description,
                Status: course.Status.ToString(),
                FreeFlowMode: course.FreeFlowMode,
                PrerequisiteCourseIds: course.PrerequisiteCourseIds,
                IsLocked: false, // Will be calculated based on student context
                CompletionPercentage: 0, // Will be calculated based on student progress
                CreatedAt: course.CreatedAt,
                UpdatedAt: course.UpdatedAt
            );

            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Creates a new course.
    /// </summary>
    /// <param name="request">Course creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created course details.</returns>
    [HttpPost]
    [Authorize(Roles = "CompanyAdmin,Instructor")]
    [ProducesResponseType(typeof(CourseResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CourseResponse>> CreateCourse(
        [FromBody] CreateCourseRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateCourseCommand(
            Name: request.Name,
            Description: request.Description,
            FreeFlowMode: request.FreeFlowMode,
            PrerequisiteCourseIds: request.PrerequisiteCourseIds ?? new List<Guid>()
        );

        var course = await _mediator.Send(command, cancellationToken);

        var response = new CourseResponse(
            Id: course.Id,
            Name: course.Name,
            Description: course.Description,
            Status: course.Status,
            FreeFlowMode: course.FreeFlowMode,
            PrerequisiteCourseIds: course.PrerequisiteCourseIds,
            IsLocked: false,
            CompletionPercentage: 0,
            CreatedAt: course.CreatedAt,
            UpdatedAt: null
        );

        return CreatedAtAction(
            nameof(GetCourse),
            new { id = course.Id },
            response
        );
    }

    /// <summary>
    /// Updates an existing course.
    /// </summary>
    /// <param name="id">Course ID.</param>
    /// <param name="request">Course update request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated course details.</returns>
    [HttpPut("{id}")]
    [Authorize(Roles = "CompanyAdmin,Instructor")]
    [ProducesResponseType(typeof(CourseResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CourseResponse>> UpdateCourse(
        Guid id,
        [FromBody] UpdateCourseRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new UpdateCourseCommand(
                Id: id,
                Name: request.Name,
                Description: request.Description,
                FreeFlowMode: request.FreeFlowMode,
                PrerequisiteCourseIds: request.PrerequisiteCourseIds ?? new List<Guid>()
            );

            var course = await _mediator.Send(command, cancellationToken);

            var response = new CourseResponse(
                Id: course.Id,
                Name: course.Name,
                Description: course.Description,
                Status: course.Status,
                FreeFlowMode: course.FreeFlowMode,
                PrerequisiteCourseIds: course.PrerequisiteCourseIds,
                IsLocked: false,
                CompletionPercentage: 0,
                CreatedAt: DateTime.UtcNow, // Not available in UpdateCourseResult
                UpdatedAt: course.UpdatedAt
            );

            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Soft deletes a course and cascades to related entities.
    /// </summary>
    /// <param name="id">Course ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpDelete("{id}")]
    [Authorize(Roles = "CompanyAdmin,Instructor")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteCourse(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new DeleteCourseCommand(id);
            await _mediator.Send(command, cancellationToken);

            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Publishes a course, making it visible to students.
    /// </summary>
    /// <param name="id">Course ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Published course details.</returns>
    [HttpPost("{id}/publish")]
    [Authorize(Roles = "CompanyAdmin,Instructor")]
    [ProducesResponseType(typeof(CourseResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CourseResponse>> PublishCourse(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new PublishCourseCommand(id);
            var result = await _mediator.Send(command, cancellationToken);

            var response = new CourseResponse(
                Id: result.Id,
                Name: "", // Not available in PublishCourseResult
                Description: "", // Not available in PublishCourseResult
                Status: result.Status,
                FreeFlowMode: false, // Not available in PublishCourseResult
                PrerequisiteCourseIds: new List<Guid>(), // Not available in PublishCourseResult
                IsLocked: false,
                CompletionPercentage: 0,
                CreatedAt: DateTime.UtcNow, // Not available in PublishCourseResult
                UpdatedAt: result.UpdatedAt
            );

            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
