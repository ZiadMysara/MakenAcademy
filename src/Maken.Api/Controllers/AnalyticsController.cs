using Maken.Api.DTOs.Responses;
using Maken.Application.Queries.Analytics;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maken.Api.Controllers;

/// <summary>
/// Controller for analytics operations.
/// Provides endpoints for viewing tenant-scoped analytics data.
/// </summary>
[ApiController]
[Route("api/analytics")]
[Authorize(Roles = "CompanyAdmin")]
public sealed class AnalyticsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AnalyticsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Gets analytics data for the current tenant.
    /// Includes enrollment counts, completion rates, and exam pass rates.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Analytics data scoped to the current tenant.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(AnalyticsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AnalyticsResponse>> GetAnalytics(CancellationToken cancellationToken)
    {
        var query = new GetAnalyticsQuery();
        var result = await _mediator.Send(query, cancellationToken);

        var response = new AnalyticsResponse(
            TotalEnrollments: result.TotalEnrollments,
            CompletionRate: result.CompletionRate,
            ExamPassRate: result.ExamPassRate
        );

        return Ok(response);
    }
}
