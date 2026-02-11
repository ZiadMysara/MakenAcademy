using Maken.Api.DTOs.Requests;
using Maken.Api.DTOs.Responses;
using Maken.Application.Commands.ContactInquiries;
using Maken.Application.Queries.ContactInquiries;
using Maken.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maken.Api.Controllers;

/// <summary>
/// Controller for managing contact inquiries from the public landing page.
/// </summary>
[ApiController]
[Route("api/contact-inquiries")]
public class ContactInquiriesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ContactInquiriesController> _logger;

    public ContactInquiriesController(IMediator mediator, ILogger<ContactInquiriesController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Submit a new contact inquiry (public endpoint).
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<ContactInquiryResponse>> CreateContactInquiry(
        [FromBody] CreateContactInquiryRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateContactInquiryCommand
        {
            ContactName = request.ContactName,
            Email = request.Email,
            OrganizationName = request.OrganizationName,
            Message = request.Message
        };

        var inquiryId = await _mediator.Send(command, cancellationToken);

        _logger.LogInformation("Contact inquiry created: {InquiryId}", inquiryId);

        return CreatedAtAction(
            nameof(CreateContactInquiry),
            new { id = inquiryId },
            new ContactInquiryResponse
            {
                Id = inquiryId,
                Message = "Your inquiry has been submitted successfully. We will contact you soon."
            });
    }

    /// <summary>
    /// Get all contact inquiries with pagination (admin only).
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "PlatformAdmin")]
    public async Task<ActionResult<ContactInquiryListResponse>> GetContactInquiries(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] ContactInquiryStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetContactInquiriesQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            Status = status
        };

        var result = await _mediator.Send(query, cancellationToken);

        return Ok(new ContactInquiryListResponse
        {
            Items = result.Items.Select(dto => new ContactInquiryResponse
            {
                Id = dto.Id,
                ContactName = dto.ContactName,
                Email = dto.Email,
                OrganizationName = dto.OrganizationName,
                Message = dto.Message,
                Status = dto.Status.ToString(),
                SubmittedAt = dto.SubmittedAt,
                ReviewedAt = dto.ReviewedAt,
                ReviewedBy = dto.ReviewedBy,
                Notes = dto.Notes
            }).ToList(),
            TotalCount = result.TotalCount,
            PageNumber = result.PageNumber,
            PageSize = result.PageSize,
            TotalPages = result.TotalPages
        });
    }

    /// <summary>
    /// Update the status of a contact inquiry (admin only).
    /// </summary>
    [HttpPatch("{id}/status")]
    [Authorize(Roles = "PlatformAdmin")]
    public async Task<IActionResult> UpdateContactInquiryStatus(
        Guid id,
        [FromBody] UpdateContactInquiryStatusRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateContactInquiryStatusCommand
        {
            Id = id,
            Status = request.Status,
            Notes = request.Notes
        };

        await _mediator.Send(command, cancellationToken);

        _logger.LogInformation("Contact inquiry status updated: {InquiryId} -> {Status}", id, request.Status);

        return NoContent();
    }
}
