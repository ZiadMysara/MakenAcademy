using FluentValidation;
using Maken.Application.Commands.Exams;

namespace Maken.Application.Validators;

/// <summary>
/// Validator for SubmitExamCommand.
/// </summary>
public class SubmitExamCommandValidator : AbstractValidator<SubmitExamCommand>
{
    public SubmitExamCommandValidator()
    {
        RuleFor(x => x.ExamId)
            .NotEmpty()
            .WithMessage("Exam ID is required.");

        RuleFor(x => x.StudentId)
            .NotEmpty()
            .WithMessage("Student ID is required.");

        RuleFor(x => x.Answers)
            .NotNull()
            .WithMessage("Answers are required.")
            .Must(answers => answers != null && answers.Count > 0)
            .WithMessage("At least one answer is required.");

        RuleForEach(x => x.Answers)
            .ChildRules(answer =>
            {
                answer.RuleFor(a => a.QuestionId)
                    .NotEmpty()
                    .WithMessage("Question ID is required for each answer.");

                answer.RuleFor(a => a.SelectedChoiceId)
                    .NotEmpty()
                    .WithMessage("Selected choice ID is required for each answer.");
            });
    }
}
