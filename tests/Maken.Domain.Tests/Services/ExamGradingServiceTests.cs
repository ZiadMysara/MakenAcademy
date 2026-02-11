using Maken.Domain.Services;

namespace Maken.Domain.Tests.Services;

/// <summary>
/// Unit tests for ExamGradingService.
/// Tests the core grading logic for exam submissions.
/// </summary>
public class ExamGradingServiceTests
{
    private readonly ExamGradingService _service;

    public ExamGradingServiceTests()
    {
        _service = new ExamGradingService();
    }

    [Fact]
    public void GradeExam_AllCorrect_ReturnsPassedWithFullScore()
    {
        // Arrange
        int totalQuestions = 10;
        int correctAnswers = 10;
        int passThreshold = 70;

        // Act
        var result = _service.GradeExam(totalQuestions, correctAnswers, passThreshold);

        // Assert
        Assert.True(result.Passed);
        Assert.Equal(100, result.Score);
        Assert.Equal(totalQuestions, result.TotalQuestions);
        Assert.Equal(correctAnswers, result.CorrectAnswers);
    }

    [Fact]
    public void GradeExam_AllWrong_ReturnsFailedWithZeroScore()
    {
        // Arrange
        int totalQuestions = 10;
        int correctAnswers = 0;
        int passThreshold = 70;

        // Act
        var result = _service.GradeExam(totalQuestions, correctAnswers, passThreshold);

        // Assert
        Assert.False(result.Passed);
        Assert.Equal(0, result.Score);
        Assert.Equal(totalQuestions, result.TotalQuestions);
        Assert.Equal(correctAnswers, result.CorrectAnswers);
    }

    [Fact]
    public void GradeExam_ExactlyAtThreshold_ReturnsPassed()
    {
        // Arrange
        int totalQuestions = 10;
        int correctAnswers = 7; // 70%
        int passThreshold = 70;

        // Act
        var result = _service.GradeExam(totalQuestions, correctAnswers, passThreshold);

        // Assert
        Assert.True(result.Passed);
        Assert.Equal(70, result.Score);
    }

    [Fact]
    public void GradeExam_JustBelowThreshold_ReturnsFailed()
    {
        // Arrange
        int totalQuestions = 10;
        int correctAnswers = 6; // 60%
        int passThreshold = 70;

        // Act
        var result = _service.GradeExam(totalQuestions, correctAnswers, passThreshold);

        // Assert
        Assert.False(result.Passed);
        Assert.Equal(60, result.Score);
    }

    [Fact]
    public void GradeExam_RoundsScoreCorrectly()
    {
        // Arrange
        int totalQuestions = 3;
        int correctAnswers = 2; // 66.67% -> rounds to 67%
        int passThreshold = 60;

        // Act
        var result = _service.GradeExam(totalQuestions, correctAnswers, passThreshold);

        // Assert
        Assert.True(result.Passed);
        Assert.Equal(67, result.Score);
    }

    [Fact]
    public void GradeExam_ZeroTotalQuestions_ThrowsArgumentException()
    {
        // Arrange
        int totalQuestions = 0;
        int correctAnswers = 0;
        int passThreshold = 70;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            _service.GradeExam(totalQuestions, correctAnswers, passThreshold));
        Assert.Contains("Total questions must be greater than 0", exception.Message);
    }

    [Fact]
    public void GradeExam_NegativeCorrectAnswers_ThrowsArgumentException()
    {
        // Arrange
        int totalQuestions = 10;
        int correctAnswers = -1;
        int passThreshold = 70;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            _service.GradeExam(totalQuestions, correctAnswers, passThreshold));
        Assert.Contains("Correct answers must be between 0 and total questions", exception.Message);
    }

    [Fact]
    public void GradeExam_CorrectAnswersExceedTotal_ThrowsArgumentException()
    {
        // Arrange
        int totalQuestions = 10;
        int correctAnswers = 11;
        int passThreshold = 70;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            _service.GradeExam(totalQuestions, correctAnswers, passThreshold));
        Assert.Contains("Correct answers must be between 0 and total questions", exception.Message);
    }

    [Fact]
    public void GradeExam_InvalidPassThresholdNegative_ThrowsArgumentException()
    {
        // Arrange
        int totalQuestions = 10;
        int correctAnswers = 7;
        int passThreshold = -1;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            _service.GradeExam(totalQuestions, correctAnswers, passThreshold));
        Assert.Contains("Pass threshold must be between 0 and 100", exception.Message);
    }

    [Fact]
    public void GradeExam_InvalidPassThresholdOver100_ThrowsArgumentException()
    {
        // Arrange
        int totalQuestions = 10;
        int correctAnswers = 7;
        int passThreshold = 101;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            _service.GradeExam(totalQuestions, correctAnswers, passThreshold));
        Assert.Contains("Pass threshold must be between 0 and 100", exception.Message);
    }

    [Fact]
    public void CalculateScore_ReturnsCorrectPercentage()
    {
        // Arrange & Act
        var score1 = _service.CalculateScore(7, 10);
        var score2 = _service.CalculateScore(5, 10);
        var score3 = _service.CalculateScore(10, 10);
        var score4 = _service.CalculateScore(0, 10);

        // Assert
        Assert.Equal(70, score1);
        Assert.Equal(50, score2);
        Assert.Equal(100, score3);
        Assert.Equal(0, score4);
    }

    [Fact]
    public void CalculateScore_ZeroTotal_ReturnsZero()
    {
        // Arrange & Act
        var score = _service.CalculateScore(0, 0);

        // Assert
        Assert.Equal(0, score);
    }

    [Fact]
    public void DeterminePassFail_ScoreAboveThreshold_ReturnsTrue()
    {
        // Arrange & Act
        var result = _service.DeterminePassFail(80, 70);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void DeterminePassFail_ScoreAtThreshold_ReturnsTrue()
    {
        // Arrange & Act
        var result = _service.DeterminePassFail(70, 70);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void DeterminePassFail_ScoreBelowThreshold_ReturnsFalse()
    {
        // Arrange & Act
        var result = _service.DeterminePassFail(69, 70);

        // Assert
        Assert.False(result);
    }
}
