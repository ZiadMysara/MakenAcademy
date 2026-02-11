using MediatR;

namespace Maken.Application.Commands.ContactInquiries;

/// <summary>
/// Command to create a new contact inquiry from the public landing page.
/// </summary>
public sealed record CreateContactInquiryCommand : IRequest<Guid>
{
    public required string ContactName { get; init; }
    public required string Email { get; init; }
    public required string OrganizationName { get; init; }
    public required string Message { get; init; }
}
