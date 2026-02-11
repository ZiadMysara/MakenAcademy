using Maken.Api.DTOs.Responses;
using Maken.Application.Queries.Tenants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maken.Api.Controllers;

/// <summary>
/// Controller for tenant operations (resolve tenant by subdomain).
/// </summary>
[ApiController]
[Route("api/tenants")]
[AllowAnonymous]
public sealed class TenantsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TenantsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Resolves a tenant by subdomain.
    /// </summary>
    /// <param name="subdomain">The tenant subdomain.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Tenant information if found.</returns>
    [HttpGet("resolve")]
    [ProducesResponseType(typeof(TenantResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TenantResponse>> ResolveTenant(
        [FromQuery] string subdomain,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(subdomain))
        {
            return BadRequest(new { message = "Subdomain is required." });
        }

        var query = new ResolveTenantQuery(subdomain);
        var result = await _mediator.Send(query, cancellationToken);

        if (result == null)
        {
            return NotFound(new { message = $"Tenant with subdomain '{subdomain}' not found." });
        }

        var response = new TenantResponse(
            Id: result.Id,
            Name: result.Name,
            Subdomain: result.Subdomain,
            IsActive: result.IsActive
        );

        return Ok(response);
    }
}
