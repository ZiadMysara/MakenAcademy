using Maken.Domain.Entities;

namespace Maken.Domain.Tests.Entities;

public class ProgressTests
{
    private readonly Guid _studentId = Guid.NewGuid();
    private readonly Guid _lessonId = Guid.NewGuid();
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidData_CreatesProgress()
    {
        // Act
        var progress = Progress.Create(_studentId, _lessonId, _tenantId);

        // Assert
        Assert.NotEqual(Guid.Empty, progress.Id);
        Assert.Equal(_studentId, progress.StudentId);
        Assert.Equal(_lessonId, progress.LessonId);
        Assert.Equal(_tenantId, progress.TenantId);
        Assert.Null(progress.CompletedAt);
        Assert.Null(progress.ExamPassed);
        Assert.Null(progress.ExamScore);
    }

    [Fact]
    public void MarkAsCompleted_SetsCompletedAt()
    {
        // Arrange
        var progress = Progress.Create(_studentId, _lessonId, _tenantId);

        // Act
        progress.MarkAsCompleted();

        // Assert
        Assert.NotNull(progress.CompletedAt);
        Assert.True(progress.CompletedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void RecordExamResult_WithPassedExam_SetsExamPassedAndScore()
    {
        // Arrange
        var progress = Progress.Create(_studentId, _lessonId, _tenantId);

        // Act
        progress.RecordExamResult(passed: true, score: 85);

        // Assert
        Assert.True(progress.ExamPassed);
        Assert.Equal(85, progress.ExamScore);
    }

    [Fact]
    public void RecordExamResult_WithFailedExam_SetsExamPassedAndScore()
    {
        // Arrange
        var progress = Progress.Create(_studentId, _lessonId, _tenantId);

        // Act
        progress.RecordExamResult(passed: false, score: 45);

        // Assert
        Assert.False(progress.ExamPassed);
        Assert.Equal(45, progress.ExamScore);
    }

    [Fact]
    public void RecordExamResult_WithPassedExam_MarksLessonAsCompleted()
    {
        // Arrange
        var progress = Progress.Create(_studentId, _lessonId, _tenantId);

        // Act
        progress.RecordExamResult(passed: true, score: 85);

        // Assert
        Assert.NotNull(progress.CompletedAt);
    }

    [Fact]
    public void RecordExamResult_WithFailedExam_DoesNotMarkLessonAsCompleted()
    {
        // Arrange
        var progress = Progress.Create(_studentId, _lessonId, _tenantId);

        // Act
        progress.RecordExamResult(passed: false, score: 45);

        // Assert
        Assert.Null(progress.CompletedAt);
    }

    [Fact]
    public void IsFullyCompleted_WithCompletedLessonAndPassedExam_ReturnsTrue()
    {
        // Arrange
        var progress = Progress.Create(_studentId, _lessonId, _tenantId);
        progress.MarkAsCompleted();
        progress.RecordExamResult(passed: true, score: 85);

        // Act
        var result = progress.IsFullyCompleted();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsFullyCompleted_WithCompletedLessonNoExam_ReturnsTrue()
    {
        // Arrange
        var progress = Progress.Create(_studentId, _lessonId, _tenantId);
        progress.MarkAsCompleted();

        // Act
        var result = progress.IsFullyCompleted();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsFullyCompleted_WithCompletedLessonButFailedExam_ReturnsFalse()
    {
        // Arrange
        var progress = Progress.Create(_studentId, _lessonId, _tenantId);
        progress.MarkAsCompleted();
        progress.RecordExamResult(passed: false, score: 45);

        // Act
        var result = progress.IsFullyCompleted();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsFullyCompleted_WithIncompletedLesson_ReturnsFalse()
    {
        // Arrange
        var progress = Progress.Create(_studentId, _lessonId, _tenantId);

        // Act
        var result = progress.IsFullyCompleted();

        // Assert
        Assert.False(result);
    }
}
