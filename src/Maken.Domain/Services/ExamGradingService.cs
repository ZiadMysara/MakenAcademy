using Maken.Domain.ValueObjects;

namespace Maken.Domain.Services;

/// <summary>
/// Domain service for grading exam submissions.
/// Implements Constitution §10.3 - Business rules in Domain layer.
/// </summary>
public class ExamGradingService
{
    /// <summary>
    /// Grades an exam submission and returns the result.
    /// </summary>
    /// <param name="totalQuestions">Total number of questions in the exam.</param>
    /// <param name="correctAnswers">Number of questions answered correctly.</param>
    /// <param name="passThreshold">Pass threshold percentage (0-100).</param>
    /// <returns>ExamResult containing pass/fail status and score.</returns>
    public ExamResult GradeExam(int totalQuestions, int correctAnswers, int passThreshold)
    {
        if (totalQuestions <= 0)
            throw new ArgumentException("Total questions must be greater than 0.", nameof(totalQuestions));

        if (correctAnswers < 0 || correctAnswers > totalQuestions)
            throw new ArgumentException("Correct answers must be between 0 and total questions.", nameof(correctAnswers));

        if (passThreshold < 0 || passThreshold > 100)
            throw new ArgumentException("Pass threshold must be between 0 and 100.", nameof(passThreshold));

        var score = CalculateScore(correctAnswers, totalQuestions);
        var passed = DeterminePassFail(score, passThreshold);

        return new ExamResult(passed, score, totalQuestions, correctAnswers);
    }

    /// <summary>
    /// Calculates the score percentage based on correct answers.
    /// </summary>
    /// <param name="correctAnswers">Number of questions answered correctly.</param>
    /// <param name="totalQuestions">Total number of questions.</param>
    /// <returns>Score as a percentage (0-100).</returns>
    public int CalculateScore(int correctAnswers, int totalQuestions)
    {
        if (totalQuestions == 0)
            return 0;

        return (int)Math.Round((double)correctAnswers / totalQuestions * 100);
    }

    /// <summary>
    /// Determines pass/fail based on score and threshold.
    /// </summary>
    /// <param name="score">The calculated score percentage (0-100).</param>
    /// <param name="passThreshold">The pass threshold percentage (0-100).</param>
    /// <returns>True if passed, false otherwise.</returns>
    public bool DeterminePassFail(int score, int passThreshold)
    {
        return score >= passThreshold;
    }
}
