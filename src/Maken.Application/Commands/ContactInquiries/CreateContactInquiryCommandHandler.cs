using Maken.Application.Common.Interfaces;
using Maken.Domain.Entities;
using MediatR;

namespace Maken.Application.Commands.ContactInquiries;

/// <summary>
/// Handler for CreateContactInquiryCommand.
/// Creates a new contact inquiry in the database.
/// </summary>
public sealed class CreateContactInquiryCommandHandler : IRequestHandler<CreateContactInquiryCommand, Guid>
{
    private readonly IRepository<ContactInquiry> _contactInquiryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateContactInquiryCommandHandler(
        IRepository<ContactInquiry> contactInquiryRepository,
        IUnitOfWork unitOfWork)
    {
        _contactInquiryRepository = contactInquiryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateContactInquiryCommand request, CancellationToken cancellationToken)
    {
        var contactInquiry = ContactInquiry.Create(
            request.ContactName,
            request.Email,
            request.OrganizationName,
            request.Message);

        await _contactInquiryRepository.AddAsync(contactInquiry, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return contactInquiry.Id;
    }
}
