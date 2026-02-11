using Maken.Api.DTOs.Responses;
using Maken.Application.Queries.Organizations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maken.Api.Controllers;

/// <summary>
/// Controller for public endpoints (no authentication required).
/// Used by the landing page to display public information.
/// </summary>
[ApiController]
[Route("api/public")]
[AllowAnonymous]
public sealed class PublicController : ControllerBase
{
    private readonly IMediator _mediator;

    public PublicController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Gets all active organizations for display on the landing page.
    /// No authentication required - returns only non-sensitive public data.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of active organizations.</returns>
    [HttpGet("organizations")]
    [ResponseCache(Duration = 300)] // Cache for 5 minutes
    [ProducesResponseType(typeof(List<PublicOrganizationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<PublicOrganizationResponse>>> GetOrganizations(
        CancellationToken cancellationToken)
    {
        var query = new GetPublicOrganizationsQuery();
        var organizations = await _mediator.Send(query, cancellationToken);

        // Map DTOs to Response objects
        var response = organizations.Select(org => new PublicOrganizationResponse
        {
            Id = org.Id,
            Name = org.Name,
            Subdomain = org.Subdomain,
            LogoUrl = org.LogoUrl,
            PrimaryColor = org.PrimaryColor,
            SecondaryColor = org.SecondaryColor
        }).ToList();

        return Ok(response);
    }
}
