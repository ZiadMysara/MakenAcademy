using Maken.Domain.Entities;

namespace Maken.Domain.Tests.Entities;

public class QuestionTests
{
    private readonly Guid _examId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidData_CreatesQuestion()
    {
        // Act
        var question = Question.Create(_examId, "What is 2+2?", 1);

        // Assert
        Assert.NotEqual(Guid.Empty, question.Id);
        Assert.Equal(_examId, question.ExamId);
        Assert.Equal("What is 2+2?", question.Text);
        Assert.Equal(1, question.Order);
    }

    [Fact]
    public void Create_WithEmptyExamId_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            Question.Create(Guid.Empty, "What is 2+2?", 1));
        Assert.Equal("examId", exception.ParamName);
    }

    [Fact]
    public void Create_WithEmptyText_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            Question.Create(_examId, "", 1));
        Assert.Equal("text", exception.ParamName);
    }

    [Fact]
    public void Create_WithTextTooLong_ThrowsArgumentException()
    {
        // Arrange
        var longText = new string('a', 1001);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            Question.Create(_examId, longText, 1));
        Assert.Equal("text", exception.ParamName);
    }

    [Fact]
    public void Create_WithZeroOrder_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            Question.Create(_examId, "What is 2+2?", 0));
        Assert.Equal("order", exception.ParamName);
    }

    [Fact]
    public void Create_WithNegativeOrder_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            Question.Create(_examId, "What is 2+2?", -1));
        Assert.Equal("order", exception.ParamName);
    }

    [Fact]
    public void SetText_WithValidText_UpdatesText()
    {
        // Arrange
        var question = Question.Create(_examId, "What is 2+2?", 1);

        // Act
        question.SetText("What is 3+3?");

        // Assert
        Assert.Equal("What is 3+3?", question.Text);
    }

    [Fact]
    public void SetText_WithEmptyText_ThrowsArgumentException()
    {
        // Arrange
        var question = Question.Create(_examId, "What is 2+2?", 1);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => question.SetText(""));
        Assert.Equal("text", exception.ParamName);
    }

    [Fact]
    public void SetText_WithTextTooLong_ThrowsArgumentException()
    {
        // Arrange
        var question = Question.Create(_examId, "What is 2+2?", 1);
        var longText = new string('a', 1001);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => question.SetText(longText));
        Assert.Equal("text", exception.ParamName);
    }

    [Fact]
    public void SetOrder_WithValidOrder_UpdatesOrder()
    {
        // Arrange
        var question = Question.Create(_examId, "What is 2+2?", 1);

        // Act
        question.SetOrder(2);

        // Assert
        Assert.Equal(2, question.Order);
    }

    [Fact]
    public void SetOrder_WithZeroOrder_ThrowsArgumentException()
    {
        // Arrange
        var question = Question.Create(_examId, "What is 2+2?", 1);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => question.SetOrder(0));
        Assert.Equal("order", exception.ParamName);
    }

    [Fact]
    public void SetOrder_WithNegativeOrder_ThrowsArgumentException()
    {
        // Arrange
        var question = Question.Create(_examId, "What is 2+2?", 1);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => question.SetOrder(-1));
        Assert.Equal("order", exception.ParamName);
    }
}
