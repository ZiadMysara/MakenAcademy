using FluentAssertions;
using Maken.Domain.Services;
using Xunit;

namespace Maken.Domain.Tests.Services;

/// <summary>
/// Unit tests for ProgressionService.
/// Tests lesson unlock, course unlock, exam unlock, and course completion calculation logic.
/// </summary>
public sealed class ProgressionServiceTests
{
    private readonly ProgressionService _sut;

    public ProgressionServiceTests()
    {
        _sut = new ProgressionService();
    }

    #region IsLessonUnlocked Tests

    [Fact]
    public void IsLessonUnlocked_FirstLesson_ShouldAlwaysBeUnlocked()
    {
        // Arrange
        var lessonOrder = 1;
        var freeFlowMode = false;
        var previousLessonCompleted = false;
        bool? previousLessonExamPassed = null;

        // Act
        var result = _sut.IsLessonUnlocked(lessonOrder, freeFlowMode, previousLessonCompleted, previousLessonExamPassed);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsLessonUnlocked_FreeFlowMode_ShouldUnlockAllLessons()
    {
        // Arrange
        var lessonOrder = 5;
        var freeFlowMode = true;
        var previousLessonCompleted = false;
        bool? previousLessonExamPassed = false;

        // Act
        var result = _sut.IsLessonUnlocked(lessonOrder, freeFlowMode, previousLessonCompleted, previousLessonExamPassed);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsLessonUnlocked_PreviousLessonNotCompleted_ShouldBeLocked()
    {
        // Arrange
        var lessonOrder = 2;
        var freeFlowMode = false;
        var previousLessonCompleted = false;
        bool? previousLessonExamPassed = null;

        // Act
        var result = _sut.IsLessonUnlocked(lessonOrder, freeFlowMode, previousLessonCompleted, previousLessonExamPassed);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsLessonUnlocked_PreviousLessonCompletedNoExam_ShouldBeUnlocked()
    {
        // Arrange
        var lessonOrder = 2;
        var freeFlowMode = false;
        var previousLessonCompleted = true;
        bool? previousLessonExamPassed = null; // No exam

        // Act
        var result = _sut.IsLessonUnlocked(lessonOrder, freeFlowMode, previousLessonCompleted, previousLessonExamPassed);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsLessonUnlocked_PreviousLessonCompletedExamPassed_ShouldBeUnlocked()
    {
        // Arrange
        var lessonOrder = 2;
        var freeFlowMode = false;
        var previousLessonCompleted = true;
        bool? previousLessonExamPassed = true;

        // Act
        var result = _sut.IsLessonUnlocked(lessonOrder, freeFlowMode, previousLessonCompleted, previousLessonExamPassed);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsLessonUnlocked_PreviousLessonCompletedExamFailed_ShouldBeLocked()
    {
        // Arrange
        var lessonOrder = 2;
        var freeFlowMode = false;
        var previousLessonCompleted = true;
        bool? previousLessonExamPassed = false;

        // Act
        var result = _sut.IsLessonUnlocked(lessonOrder, freeFlowMode, previousLessonCompleted, previousLessonExamPassed);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(10)]
    [InlineData(100)]
    public void IsLessonUnlocked_FirstLessonWithAnyOrder_ShouldAlwaysBeUnlocked(int lessonOrder)
    {
        // Arrange
        var freeFlowMode = false;
        var previousLessonCompleted = false;
        bool? previousLessonExamPassed = null;

        // Act
        var result = _sut.IsLessonUnlocked(1, freeFlowMode, previousLessonCompleted, previousLessonExamPassed);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(10)]
    public void IsLessonUnlocked_FreeFlowModeWithAnyOrder_ShouldAlwaysBeUnlocked(int lessonOrder)
    {
        // Arrange
        var freeFlowMode = true;
        var previousLessonCompleted = false;
        bool? previousLessonExamPassed = false;

        // Act
        var result = _sut.IsLessonUnlocked(lessonOrder, freeFlowMode, previousLessonCompleted, previousLessonExamPassed);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region IsCourseUnlocked Tests

    [Fact]
    public void IsCourseUnlocked_NoPrerequisites_ShouldAlwaysBeUnlocked()
    {
        // Arrange
        var hasPrerequisites = false;
        var allPrerequisitesCompleted = false;

        // Act
        var result = _sut.IsCourseUnlocked(hasPrerequisites, allPrerequisitesCompleted);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsCourseUnlocked_HasPrerequisitesAllCompleted_ShouldBeUnlocked()
    {
        // Arrange
        var hasPrerequisites = true;
        var allPrerequisitesCompleted = true;

        // Act
        var result = _sut.IsCourseUnlocked(hasPrerequisites, allPrerequisitesCompleted);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsCourseUnlocked_HasPrerequisitesNotAllCompleted_ShouldBeLocked()
    {
        // Arrange
        var hasPrerequisites = true;
        var allPrerequisitesCompleted = false;

        // Act
        var result = _sut.IsCourseUnlocked(hasPrerequisites, allPrerequisitesCompleted);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region IsExamUnlocked Tests

    [Fact]
    public void IsExamUnlocked_LessonViewed_ShouldBeUnlocked()
    {
        // Arrange
        var lessonViewed = true;

        // Act
        var result = _sut.IsExamUnlocked(lessonViewed);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsExamUnlocked_LessonNotViewed_ShouldBeLocked()
    {
        // Arrange
        var lessonViewed = false;

        // Act
        var result = _sut.IsExamUnlocked(lessonViewed);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region CalculateCourseCompletion Tests

    [Fact]
    public void CalculateCourseCompletion_NoLessons_ShouldReturnZero()
    {
        // Arrange
        var totalLessons = 0;
        var completedLessons = 0;

        // Act
        var result = _sut.CalculateCourseCompletion(totalLessons, completedLessons);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void CalculateCourseCompletion_NoCompletedLessons_ShouldReturnZero()
    {
        // Arrange
        var totalLessons = 10;
        var completedLessons = 0;

        // Act
        var result = _sut.CalculateCourseCompletion(totalLessons, completedLessons);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void CalculateCourseCompletion_AllLessonsCompleted_ShouldReturn100()
    {
        // Arrange
        var totalLessons = 10;
        var completedLessons = 10;

        // Act
        var result = _sut.CalculateCourseCompletion(totalLessons, completedLessons);

        // Assert
        result.Should().Be(100);
    }

    [Fact]
    public void CalculateCourseCompletion_HalfCompleted_ShouldReturn50()
    {
        // Arrange
        var totalLessons = 10;
        var completedLessons = 5;

        // Act
        var result = _sut.CalculateCourseCompletion(totalLessons, completedLessons);

        // Assert
        result.Should().Be(50);
    }

    [Fact]
    public void CalculateCourseCompletion_PartialCompletion_ShouldReturnCorrectPercentage()
    {
        // Arrange
        var totalLessons = 7;
        var completedLessons = 3;

        // Act
        var result = _sut.CalculateCourseCompletion(totalLessons, completedLessons);

        // Assert
        result.Should().BeApproximately(42.86m, 0.01m);
    }

    [Theory]
    [InlineData(10, 1, 10)]
    [InlineData(10, 2, 20)]
    [InlineData(10, 3, 30)]
    [InlineData(10, 9, 90)]
    [InlineData(20, 5, 25)]
    [InlineData(4, 1, 25)]
    public void CalculateCourseCompletion_VariousScenarios_ShouldReturnCorrectPercentage(
        int totalLessons, int completedLessons, decimal expectedPercentage)
    {
        // Act
        var result = _sut.CalculateCourseCompletion(totalLessons, completedLessons);

        // Assert
        result.Should().Be(expectedPercentage);
    }

    [Fact]
    public void CalculateCourseCompletion_ShouldRoundToTwoDecimalPlaces()
    {
        // Arrange
        var totalLessons = 3;
        var completedLessons = 1;

        // Act
        var result = _sut.CalculateCourseCompletion(totalLessons, completedLessons);

        // Assert
        result.Should().Be(33.33m);
    }

    #endregion

    #region Edge Cases and Complex Scenarios

    [Fact]
    public void IsLessonUnlocked_Lesson10WithAllPreviousCompleted_ShouldBeUnlocked()
    {
        // Arrange
        var lessonOrder = 10;
        var freeFlowMode = false;
        var previousLessonCompleted = true;
        bool? previousLessonExamPassed = true;

        // Act
        var result = _sut.IsLessonUnlocked(lessonOrder, freeFlowMode, previousLessonCompleted, previousLessonExamPassed);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CalculateCourseCompletion_SingleLesson_ShouldReturnCorrectPercentage()
    {
        // Arrange & Act
        var result0 = _sut.CalculateCourseCompletion(1, 0);
        var result1 = _sut.CalculateCourseCompletion(1, 1);

        // Assert
        result0.Should().Be(0);
        result1.Should().Be(100);
    }

    [Fact]
    public void CalculateCourseCompletion_LargeNumberOfLessons_ShouldHandleCorrectly()
    {
        // Arrange
        var totalLessons = 1000;
        var completedLessons = 750;

        // Act
        var result = _sut.CalculateCourseCompletion(totalLessons, completedLessons);

        // Assert
        result.Should().Be(75);
    }

    #endregion
}
