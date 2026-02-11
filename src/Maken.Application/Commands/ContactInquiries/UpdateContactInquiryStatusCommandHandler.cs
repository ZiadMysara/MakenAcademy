using Maken.Application.Common.Interfaces;
using Maken.Domain.Entities;
using MediatR;

namespace Maken.Application.Commands.ContactInquiries;

/// <summary>
/// Handler for UpdateContactInquiryStatusCommand.
/// Updates the status and notes of a contact inquiry.
/// </summary>
public sealed class UpdateContactInquiryStatusCommandHandler : IRequestHandler<UpdateContactInquiryStatusCommand, Unit>
{
    private readonly IRepository<ContactInquiry> _contactInquiryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateContactInquiryStatusCommandHandler(
        IRepository<ContactInquiry> contactInquiryRepository,
        IUnitOfWork unitOfWork)
    {
        _contactInquiryRepository = contactInquiryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(UpdateContactInquiryStatusCommand request, CancellationToken cancellationToken)
    {
        var inquiry = await _contactInquiryRepository.GetByIdAsync(request.Id, cancellationToken);
        
        if (inquiry == null)
            throw new InvalidOperationException($"Contact inquiry with ID {request.Id} not found.");

        inquiry.UpdateStatus(request.Status, Guid.Empty, request.Notes);

        _contactInquiryRepository.Update(inquiry);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
