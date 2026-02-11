using FluentValidation;

namespace Maken.Application.Commands.ContactInquiries;

/// <summary>
/// Validator for CreateContactInquiryCommand.
/// </summary>
public sealed class CreateContactInquiryCommandValidator : AbstractValidator<CreateContactInquiryCommand>
{
    public CreateContactInquiryCommandValidator()
    {
        RuleFor(x => x.ContactName)
            .NotEmpty().WithMessage("Contact name is required.")
            .MaximumLength(100).WithMessage("Contact name must not exceed 100 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email must be a valid email address.")
            .MaximumLength(255).WithMessage("Email must not exceed 255 characters.");

        RuleFor(x => x.OrganizationName)
            .NotEmpty().WithMessage("Organization name is required.")
            .MaximumLength(200).WithMessage("Organization name must not exceed 200 characters.");

        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("Message is required.")
            .MinimumLength(10).WithMessage("Message must be at least 10 characters.")
            .MaximumLength(2000).WithMessage("Message must not exceed 2000 characters.");
    }
}
