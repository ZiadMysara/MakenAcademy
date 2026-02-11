using Maken.Application.Commands.Students;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maken.Api.Controllers;

/// <summary>
/// Controller for student-specific operations.
/// </summary>
[ApiController]
[Route("api/students")]
[Authorize(Roles = "Student")]
public class StudentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public StudentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Marks a lesson as completed for the current student.
    /// </summary>
    [HttpPost("lessons/{lessonId}/complete")]
    public async Task<IActionResult> CompleteLesson(
        Guid lessonId,
        CancellationToken cancellationToken)
    {
        // Get current user ID from claims
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var studentId))
        {
            return Unauthorized(new { message = "User ID not found in token." });
        }

        var command = new CompleteLessonCommand(studentId, lessonId);
        await _mediator.Send(command, cancellationToken);

        return Ok(new { message = "Lesson marked as complete." });
    }
}
