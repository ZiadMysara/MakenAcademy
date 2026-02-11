using FluentValidation;
using Maken.Application.Commands.Lessons;

namespace Maken.Application.Validators;

/// <summary>
/// Validator for UpdateLessonCommand.
/// </summary>
public sealed class UpdateLessonCommandValidator : AbstractValidator<UpdateLessonCommand>
{
    public UpdateLessonCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Lesson ID is required");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Lesson title is required")
            .MaximumLength(200).WithMessage("Lesson title cannot exceed 200 characters");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Lesson description is required")
            .MaximumLength(2000).WithMessage("Lesson description cannot exceed 2000 characters");

        RuleFor(x => x.ContentType)
            .NotEmpty().WithMessage("Content type is required")
            .Must(ct => ct == "Video" || ct == "PDF")
            .WithMessage("Content type must be either 'Video' or 'PDF'");

        RuleFor(x => x.ContentUrl)
            .NotEmpty().WithMessage("Content URL is required")
            .MaximumLength(500).WithMessage("Content URL cannot exceed 500 characters");

        RuleFor(x => x.Order)
            .GreaterThan(0).WithMessage("Order must be a positive integer");
    }
}
