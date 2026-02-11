using Maken.Application.Common.Interfaces;
using Maken.Application.DTOs;
using Maken.Domain.Entities;
using MediatR;

namespace Maken.Application.Queries.ContactInquiries;

/// <summary>
/// Handler for GetContactInquiriesQuery.
/// Retrieves paginated contact inquiries with optional status filtering.
/// </summary>
public sealed class GetContactInquiriesQueryHandler : IRequestHandler<GetContactInquiriesQuery, ContactInquiriesResult>
{
    private readonly IRepository<ContactInquiry> _contactInquiryRepository;

    public GetContactInquiriesQueryHandler(IRepository<ContactInquiry> contactInquiryRepository)
    {
        _contactInquiryRepository = contactInquiryRepository;
    }

    public async Task<ContactInquiriesResult> Handle(GetContactInquiriesQuery request, CancellationToken cancellationToken)
    {
        // Get all inquiries with optional status filter
        var allInquiries = await _contactInquiryRepository.GetAllAsync(
            filter: request.Status.HasValue 
                ? i => i.Status == request.Status.Value 
                : null,
            cancellationToken: cancellationToken);

        // Order by submitted date descending
        var orderedInquiries = allInquiries.OrderByDescending(i => i.SubmittedAt).ToList();

        // Calculate pagination
        var totalCount = orderedInquiries.Count;
        var skip = (request.PageNumber - 1) * request.PageSize;
        var pagedInquiries = orderedInquiries.Skip(skip).Take(request.PageSize).ToList();

        // Map to DTOs
        var dtos = pagedInquiries.Select(i => new ContactInquiryDto
        {
            Id = i.Id,
            ContactName = i.ContactName,
            Email = i.Email,
            OrganizationName = i.OrganizationName,
            Message = i.Message,
            Status = i.Status,
            SubmittedAt = i.SubmittedAt,
            ReviewedAt = i.ReviewedAt,
            ReviewedBy = i.ReviewedBy,
            Notes = i.Notes
        }).ToList();

        return new ContactInquiriesResult
        {
            Items = dtos,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}
