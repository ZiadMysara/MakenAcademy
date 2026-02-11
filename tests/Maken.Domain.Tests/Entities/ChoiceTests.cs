using Maken.Domain.Entities;

namespace Maken.Domain.Tests.Entities;

public class ChoiceTests
{
    private readonly Guid _questionId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidData_CreatesChoice()
    {
        // Act
        var choice = Choice.Create(_questionId, "Answer A", true);

        // Assert
        Assert.NotEqual(Guid.Empty, choice.Id);
        Assert.Equal(_questionId, choice.QuestionId);
        Assert.Equal("Answer A", choice.Text);
        Assert.True(choice.IsCorrect);
    }

    [Fact]
    public void Create_WithEmptyQuestionId_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            Choice.Create(Guid.Empty, "Answer A", true));
        Assert.Equal("questionId", exception.ParamName);
    }

    [Fact]
    public void Create_WithEmptyText_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            Choice.Create(_questionId, "", true));
        Assert.Equal("text", exception.ParamName);
    }

    [Fact]
    public void Create_WithTextTooLong_ThrowsArgumentException()
    {
        // Arrange
        var longText = new string('a', 501);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            Choice.Create(_questionId, longText, true));
        Assert.Equal("text", exception.ParamName);
    }

    [Fact]
    public void Create_WithIsCorrectFalse_CreatesIncorrectChoice()
    {
        // Act
        var choice = Choice.Create(_questionId, "Answer B", false);

        // Assert
        Assert.False(choice.IsCorrect);
    }

    [Fact]
    public void SetText_WithValidText_UpdatesText()
    {
        // Arrange
        var choice = Choice.Create(_questionId, "Answer A", true);

        // Act
        choice.SetText("Answer B");

        // Assert
        Assert.Equal("Answer B", choice.Text);
    }

    [Fact]
    public void SetText_WithEmptyText_ThrowsArgumentException()
    {
        // Arrange
        var choice = Choice.Create(_questionId, "Answer A", true);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => choice.SetText(""));
        Assert.Equal("text", exception.ParamName);
    }

    [Fact]
    public void SetText_WithTextTooLong_ThrowsArgumentException()
    {
        // Arrange
        var choice = Choice.Create(_questionId, "Answer A", true);
        var longText = new string('a', 501);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => choice.SetText(longText));
        Assert.Equal("text", exception.ParamName);
    }

    [Fact]
    public void SetIsCorrect_WithTrue_UpdatesIsCorrect()
    {
        // Arrange
        var choice = Choice.Create(_questionId, "Answer A", false);

        // Act
        choice.SetIsCorrect(true);

        // Assert
        Assert.True(choice.IsCorrect);
    }

    [Fact]
    public void SetIsCorrect_WithFalse_UpdatesIsCorrect()
    {
        // Arrange
        var choice = Choice.Create(_questionId, "Answer A", true);

        // Act
        choice.SetIsCorrect(false);

        // Assert
        Assert.False(choice.IsCorrect);
    }
}
