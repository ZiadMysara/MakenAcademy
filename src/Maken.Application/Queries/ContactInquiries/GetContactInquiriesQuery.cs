using Maken.Application.DTOs;
using Maken.Domain.Entities;
using MediatR;

namespace Maken.Application.Queries.ContactInquiries;

/// <summary>
/// Query to get contact inquiries with pagination and filtering.
/// </summary>
public sealed record GetContactInquiriesQuery : IRequest<ContactInquiriesResult>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public ContactInquiryStatus? Status { get; init; }
}

/// <summary>
/// Result containing paginated contact inquiries.
/// </summary>
public sealed record ContactInquiriesResult
{
    public required List<ContactInquiryDto> Items { get; init; }
    public required int TotalCount { get; init; }
    public required int PageNumber { get; init; }
    public required int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
