using FluentValidation;
using Maken.Application.Commands.Courses;

namespace Maken.Application.Validators;

/// <summary>
/// Validator for UpdateCourseCommand.
/// Ensures course ID is valid and properties meet requirements.
/// </summary>
public sealed class UpdateCourseCommandValidator : AbstractValidator<UpdateCourseCommand>
{
    public UpdateCourseCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Course ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Course name is required.")
            .MaximumLength(200).WithMessage("Course name cannot exceed 200 characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Course description is required.")
            .MaximumLength(2000).WithMessage("Course description cannot exceed 2000 characters.");

        RuleFor(x => x.PrerequisiteCourseIds)
            .Must(ids => ids == null || ids.All(id => id != Guid.Empty))
            .WithMessage("Prerequisite course IDs must be valid GUIDs.");
    }
}
