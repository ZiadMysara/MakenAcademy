namespace Maken.Domain.ValueObjects;

/// <summary>
/// Value object representing the result of an exam submission.
/// </summary>
public sealed record ExamResult
{
    /// <summary>
    /// Whether the student passed the exam based on the pass threshold.
    /// </summary>
    public bool Passed { get; init; }

    /// <summary>
    /// The score as a percentage (0-100).
    /// </summary>
    public int Score { get; init; }

    /// <summary>
    /// Total number of questions in the exam.
    /// </summary>
    public int TotalQuestions { get; init; }

    /// <summary>
    /// Number of questions answered correctly.
    /// </summary>
    public int CorrectAnswers { get; init; }

    /// <summary>
    /// Creates a new exam result.
    /// </summary>
    public ExamResult(bool passed, int score, int totalQuestions, int correctAnswers)
    {
        if (score < 0 || score > 100)
            throw new ArgumentException("Score must be between 0 and 100.", nameof(score));

        if (totalQuestions <= 0)
            throw new ArgumentException("Total questions must be greater than 0.", nameof(totalQuestions));

        if (correctAnswers < 0 || correctAnswers > totalQuestions)
            throw new ArgumentException("Correct answers must be between 0 and total questions.", nameof(correctAnswers));

        Passed = passed;
        Score = score;
        TotalQuestions = totalQuestions;
        CorrectAnswers = correctAnswers;
    }
}
