using Maken.Domain.Common;

namespace Maken.Domain.Entities;

/// <summary>
/// Represents a multiple-choice question within an exam.
/// Questions are ordered sequentially and contain multiple choices.
/// </summary>
public sealed class Question : BaseEntity
{
    public Guid ExamId { get; private set; }
    public string Text { get; private set; } = string.Empty;
    public int Order { get; private set; }

    // Navigation properties
    public Exam? Exam { get; private set; }
    public ICollection<Choice> Choices { get; private set; } = new List<Choice>();

    private Question() { }

    /// <summary>
    /// Creates a new question for an exam.
    /// </summary>
    /// <param name="examId">The ID of the exam this question belongs to.</param>
    /// <param name="text">Question text (1-1000 characters).</param>
    /// <param name="order">Question order within the exam (must be positive).</param>
    public static Question Create(Guid examId, string text, int order)
    {
        if (examId == Guid.Empty)
            throw new ArgumentException("Exam ID cannot be empty", nameof(examId));
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Question text cannot be empty", nameof(text));
        if (text.Length > 1000)
            throw new ArgumentException("Question text cannot exceed 1000 characters", nameof(text));
        if (order < 1)
            throw new ArgumentException("Order must be a positive integer", nameof(order));

        return new Question
        {
            ExamId = examId,
            Text = text.Trim(),
            Order = order
        };
    }

    /// <summary>
    /// Updates the question text.
    /// </summary>
    public void SetText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Question text cannot be empty", nameof(text));
        if (text.Length > 1000)
            throw new ArgumentException("Question text cannot exceed 1000 characters", nameof(text));

        Text = text.Trim();
    }

    /// <summary>
    /// Updates the question order.
    /// </summary>
    public void SetOrder(int order)
    {
        if (order < 1)
            throw new ArgumentException("Order must be a positive integer", nameof(order));

        Order = order;
    }
}
