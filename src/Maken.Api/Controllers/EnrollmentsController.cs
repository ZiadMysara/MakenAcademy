using Maken.Api.DTOs.Requests;
using Maken.Api.DTOs.Responses;
using Maken.Application.Commands.Students;
using Maken.Application.Queries.Enrollments;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maken.Api.Controllers;

/// <summary>
/// Controller for student enrollment operations.
/// Provides endpoints for enrolling students in courses and viewing enrollments.
/// </summary>
[ApiController]
[Route("api/courses/{courseId}/enrollments")]
[Authorize]
public sealed class EnrollmentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public EnrollmentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Gets all enrollments for a specific course.
    /// </summary>
    /// <param name="courseId">Course ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of enrollments with student information.</returns>
    [HttpGet]
    [Authorize(Roles = "CompanyAdmin,Instructor")]
    [ProducesResponseType(typeof(List<EnrollmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<EnrollmentResponse>>> GetEnrollments(
        Guid courseId,
        CancellationToken cancellationToken)
    {
        try
        {
            var query = new GetEnrollmentsQuery(courseId);
            var enrollments = await _mediator.Send(query, cancellationToken);

            var response = enrollments.Select(e => new EnrollmentResponse(
                Id: e.Id,
                StudentId: e.StudentId,
                StudentFirstName: e.StudentFirstName,
                StudentLastName: e.StudentLastName,
                StudentEmail: e.StudentEmail,
                CourseId: e.CourseId,
                EnrolledAt: e.EnrolledAt,
                CompletedAt: e.CompletedAt
            )).ToList();

            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid();
        }
    }

    /// <summary>
    /// Enrolls a student in a course.
    /// </summary>
    /// <param name="courseId">Course ID.</param>
    /// <param name="request">Enrollment request containing student ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created enrollment details.</returns>
    [HttpPost]
    [Authorize(Roles = "CompanyAdmin,Instructor")]
    [ProducesResponseType(typeof(EnrollmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<EnrollmentResponse>> EnrollStudent(
        Guid courseId,
        [FromBody] EnrollStudentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new EnrollStudentCommand(
                StudentId: request.StudentId,
                CourseId: courseId
            );

            var result = await _mediator.Send(command, cancellationToken);

            // Fetch student information for response
            var query = new GetEnrollmentsQuery(courseId);
            var enrollments = await _mediator.Send(query, cancellationToken);
            var enrollment = enrollments.FirstOrDefault(e => e.Id == result.Id);

            if (enrollment == null)
            {
                // Fallback response without student details
                var fallbackResponse = new EnrollmentResponse(
                    Id: result.Id,
                    StudentId: result.StudentId,
                    StudentFirstName: string.Empty,
                    StudentLastName: string.Empty,
                    StudentEmail: string.Empty,
                    CourseId: result.CourseId,
                    EnrolledAt: result.EnrolledAt,
                    CompletedAt: null
                );
                return CreatedAtAction(
                    nameof(GetEnrollments),
                    new { courseId = result.CourseId },
                    fallbackResponse
                );
            }

            var response = new EnrollmentResponse(
                Id: enrollment.Id,
                StudentId: enrollment.StudentId,
                StudentFirstName: enrollment.StudentFirstName,
                StudentLastName: enrollment.StudentLastName,
                StudentEmail: enrollment.StudentEmail,
                CourseId: enrollment.CourseId,
                EnrolledAt: enrollment.EnrolledAt,
                CompletedAt: enrollment.CompletedAt
            );

            return CreatedAtAction(
                nameof(GetEnrollments),
                new { courseId = result.CourseId },
                response
            );
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid();
        }
    }
}
