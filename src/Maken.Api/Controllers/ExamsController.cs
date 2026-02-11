using Maken.Api.DTOs.Requests;
using Maken.Api.DTOs.Responses;
using Maken.Application.Commands.Exams;
using Maken.Application.DTOs;
using Maken.Application.Queries.Exams;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maken.Api.Controllers;

/// <summary>
/// Controller for managing exams.
/// </summary>
[ApiController]
[Route("api/exams")]
[Authorize]
public class ExamsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ExamsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Creates a new exam.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ExamResponse>> CreateExam(
        [FromBody] CreateExamRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new CreateExamCommand(
                LessonId: request.LessonId,
                Title: request.Title,
                PassThreshold: request.PassThreshold,
                Questions: request.Questions.Select(q => new CreateQuestionDto(
                    Text: q.Text,
                    Order: q.Order,
                    Choices: q.Choices.Select(c => new CreateChoiceDto(
                        Text: c.Text,
                        IsCorrect: c.IsCorrect
                    )).ToList()
                )).ToList()
            );

            var result = await _mediator.Send(command, cancellationToken);

            // Retrieve the created exam with questions included
            var getExamQuery = new GetExamQuery(result.Id, true);
            var examDto = await _mediator.Send(getExamQuery, cancellationToken);

            if (examDto == null)
            {
                return NotFound(new { message = $"Exam with ID {result.Id} not found." });
            }

            var response = MapToExamResponse(examDto);
            return CreatedAtAction(nameof(GetExam), new { id = result.Id }, response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Gets an exam by ID.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ExamResponse>> GetExam(
        Guid id,
        [FromQuery] bool includeAnswers = false,
        CancellationToken cancellationToken = default)
    {
        var query = new GetExamQuery(id, includeAnswers);
        var result = await _mediator.Send(query, cancellationToken);

        if (result == null)
        {
            return NotFound(new { message = $"Exam with ID {id} not found." });
        }

        var response = MapToExamResponse(result);
        return Ok(response);
    }

    /// <summary>
    /// Updates an existing exam.
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<ExamResponse>> UpdateExam(
        Guid id,
        [FromBody] UpdateExamRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new UpdateExamCommand(
                Id: id,
                Title: request.Title,
                PassThreshold: request.PassThreshold,
                Questions: request.Questions.Select(q => new UpdateQuestionDto(
                    Id: q.Id,
                    Text: q.Text,
                    Order: q.Order,
                    Choices: q.Choices.Select(c => new UpdateChoiceDto(
                        Id: c.Id,
                        Text: c.Text,
                        IsCorrect: c.IsCorrect
                    )).ToList()
                )).ToList()
            );

            var result = await _mediator.Send(command, cancellationToken);

            var response = new ExamResponse(
                Id: result.Id,
                LessonId: result.LessonId,
                TenantId: result.TenantId,
                Title: result.Title,
                PassThreshold: result.PassThreshold,
                Questions: new List<QuestionResponse>(),
                CreatedAt: DateTime.UtcNow,
                UpdatedAt: result.UpdatedAt
            );

            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Deletes an exam.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteExam(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new DeleteExamCommand(id);
            await _mediator.Send(command, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Gets exam for a specific lesson (student view).
    /// </summary>
    [HttpGet("/api/courses/{courseId}/lessons/{lessonId}/exam")]
    public async Task<ActionResult<ExamResponse>> GetLessonExam(
        Guid courseId,
        Guid lessonId,
        [FromQuery] bool includeAnswers = false,
        CancellationToken cancellationToken = default)
    {
        // First, get the exam by lessonId
        var examRepository = HttpContext.RequestServices.GetRequiredService<Maken.Application.Common.Interfaces.IUnitOfWork>();
        var exams = await examRepository.Repository<Maken.Domain.Entities.Exam>()
            .GetAllAsync(e => e.LessonId == lessonId, cancellationToken);
        
        var exam = exams.FirstOrDefault();
        if (exam == null)
        {
            return NotFound(new { message = $"No exam found for lesson {lessonId}." });
        }

        var query = new GetExamQuery(exam.Id, includeAnswers);
        var result = await _mediator.Send(query, cancellationToken);

        if (result == null)
        {
            return NotFound(new { message = $"Exam not found." });
        }

        var response = MapToExamResponse(result);
        return Ok(response);
    }

    /// <summary>
    /// Submits exam answers for a specific lesson.
    /// </summary>
    [HttpPost("/api/courses/{courseId}/lessons/{lessonId}/exam")]
    public async Task<ActionResult<ExamResultResponse>> SubmitLessonExam(
        Guid courseId,
        Guid lessonId,
        [FromBody] SubmitExamRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get the exam by lessonId
            var examRepository = HttpContext.RequestServices.GetRequiredService<Maken.Application.Common.Interfaces.IUnitOfWork>();
            var exams = await examRepository.Repository<Maken.Domain.Entities.Exam>()
                .GetAllAsync(e => e.LessonId == lessonId, cancellationToken);
            
            var exam = exams.FirstOrDefault();
            if (exam == null)
            {
                return NotFound(new { message = $"No exam found for lesson {lessonId}." });
            }

            // Get current user ID from claims
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var studentId))
            {
                return Unauthorized(new { message = "User ID not found in token." });
            }

            var command = new SubmitExamCommand(
                ExamId: exam.Id,
                StudentId: studentId,
                Answers: request.Answers.Select(a => new SubmitAnswerDto(
                    QuestionId: a.QuestionId,
                    SelectedChoiceId: a.SelectedChoiceId
                )).ToList()
            );

            var result = await _mediator.Send(command, cancellationToken);

            var response = new ExamResultResponse(
                ExamId: exam.Id,
                StudentId: studentId,
                Passed: result.Passed,
                Score: result.Score,
                TotalQuestions: result.TotalQuestions,
                CorrectAnswers: result.CorrectAnswers,
                AttemptedAt: DateTime.UtcNow
            );

            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private static ExamResponse MapToExamResponse(ExamDto dto)
    {
        return new ExamResponse(
            Id: dto.Id,
            LessonId: dto.LessonId,
            TenantId: dto.TenantId,
            Title: dto.Title,
            PassThreshold: dto.PassThreshold,
            Questions: dto.Questions.Select(q => new QuestionResponse(
                Id: q.Id,
                ExamId: q.ExamId,
                Text: q.Text,
                Order: q.Order,
                Choices: q.Choices.Select(c => new ChoiceResponse(
                    Id: c.Id,
                    QuestionId: c.QuestionId,
                    Text: c.Text,
                    IsCorrect: c.IsCorrect
                )).ToList()
            )).ToList(),
            CreatedAt: dto.CreatedAt,
            UpdatedAt: dto.UpdatedAt
        );
    }
}
