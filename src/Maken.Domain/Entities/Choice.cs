using Maken.Domain.Common;

namespace Maken.Domain.Entities;

/// <summary>
/// Represents a choice (answer option) for a multiple-choice question.
/// Each choice has text and a flag indicating if it's the correct answer.
/// </summary>
public sealed class Choice : BaseEntity
{
    public Guid QuestionId { get; private set; }
    public string Text { get; private set; } = string.Empty;
    public bool IsCorrect { get; private set; }

    // Navigation properties
    public Question? Question { get; private set; }

    private Choice() { }

    /// <summary>
    /// Creates a new choice for a question.
    /// </summary>
    /// <param name="questionId">The ID of the question this choice belongs to.</param>
    /// <param name="text">Choice text (1-500 characters).</param>
    /// <param name="isCorrect">Whether this choice is the correct answer.</param>
    public static Choice Create(Guid questionId, string text, bool isCorrect)
    {
        if (questionId == Guid.Empty)
            throw new ArgumentException("Question ID cannot be empty", nameof(questionId));
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Choice text cannot be empty", nameof(text));
        if (text.Length > 500)
            throw new ArgumentException("Choice text cannot exceed 500 characters", nameof(text));

        return new Choice
        {
            QuestionId = questionId,
            Text = text.Trim(),
            IsCorrect = isCorrect
        };
    }

    /// <summary>
    /// Updates the choice text.
    /// </summary>
    public void SetText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Choice text cannot be empty", nameof(text));
        if (text.Length > 500)
            throw new ArgumentException("Choice text cannot exceed 500 characters", nameof(text));

        Text = text.Trim();
    }

    /// <summary>
    /// Updates whether this choice is correct.
    /// </summary>
    public void SetIsCorrect(bool isCorrect)
    {
        IsCorrect = isCorrect;
    }
}
