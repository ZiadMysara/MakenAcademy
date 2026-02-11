using FluentValidation;
using Maken.Application.Commands.Exams;

namespace Maken.Application.Validators;

/// <summary>
/// Validator for CreateExamCommand.
/// Ensures exam structure meets requirements.
/// </summary>
public sealed class CreateExamCommandValidator : AbstractValidator<CreateExamCommand>
{
    public CreateExamCommandValidator()
    {
        RuleFor(x => x.LessonId)
            .NotEmpty().WithMessage("Lesson ID is required.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Exam title is required.")
            .MaximumLength(200).WithMessage("Exam title cannot exceed 200 characters.");

        RuleFor(x => x.PassThreshold)
            .InclusiveBetween(0, 100).WithMessage("Pass threshold must be between 0 and 100.");

        RuleFor(x => x.Questions)
            .NotEmpty().WithMessage("Exam must have at least one question.")
            .Must(questions => questions != null && questions.Count > 0)
            .WithMessage("Exam must have at least one question.");

        RuleForEach(x => x.Questions).ChildRules(question =>
        {
            question.RuleFor(q => q.Text)
                .NotEmpty().WithMessage("Question text is required.")
                .MaximumLength(1000).WithMessage("Question text cannot exceed 1000 characters.");

            question.RuleFor(q => q.Order)
                .GreaterThan(0).WithMessage("Question order must be a positive integer.");

            question.RuleFor(q => q.Choices)
                .NotEmpty().WithMessage("Question must have at least one choice.")
                .Must(choices => choices != null && choices.Count >= 2)
                .WithMessage("Question must have at least 2 choices.");

            question.RuleForEach(q => q.Choices).ChildRules(choice =>
            {
                choice.RuleFor(c => c.Text)
                    .NotEmpty().WithMessage("Choice text is required.")
                    .MaximumLength(500).WithMessage("Choice text cannot exceed 500 characters.");
            });
        });
    }
}
