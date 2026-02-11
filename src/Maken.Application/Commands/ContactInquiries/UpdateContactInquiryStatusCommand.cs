using Maken.Domain.Entities;
using MediatR;

namespace Maken.Application.Commands.ContactInquiries;

/// <summary>
/// Command to update the status of a contact inquiry.
/// </summary>
public sealed record UpdateContactInquiryStatusCommand : IRequest<Unit>
{
    public required Guid Id { get; init; }
    public required ContactInquiryStatus Status { get; init; }
    public string? Notes { get; init; }
}
