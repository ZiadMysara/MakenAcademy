using Maken.Domain.Entities;

namespace Maken.Domain.Tests.Entities;

public class ExamTests
{
    private readonly Guid _lessonId = Guid.NewGuid();
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidData_CreatesExam()
    {
        // Act
        var exam = Exam.Create(_lessonId, _tenantId, "Final Exam", 70);

        // Assert
        Assert.NotEqual(Guid.Empty, exam.Id);
        Assert.Equal(_lessonId, exam.LessonId);
        Assert.Equal(_tenantId, exam.TenantId);
        Assert.Equal("Final Exam", exam.Title);
        Assert.Equal(70, exam.PassThreshold);
    }

    [Fact]
    public void Create_WithEmptyLessonId_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            Exam.Create(Guid.Empty, _tenantId, "Final Exam", 70));
        Assert.Equal("lessonId", exception.ParamName);
    }

    [Fact]
    public void Create_WithEmptyTenantId_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            Exam.Create(_lessonId, Guid.Empty, "Final Exam", 70));
        Assert.Equal("tenantId", exception.ParamName);
    }

    [Fact]
    public void Create_WithEmptyTitle_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            Exam.Create(_lessonId, _tenantId, "", 70));
        Assert.Equal("title", exception.ParamName);
    }

    [Fact]
    public void Create_WithTitleTooLong_ThrowsArgumentException()
    {
        // Arrange
        var longTitle = new string('a', 201);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            Exam.Create(_lessonId, _tenantId, longTitle, 70));
        Assert.Equal("title", exception.ParamName);
    }

    [Fact]
    public void Create_WithNegativePassThreshold_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            Exam.Create(_lessonId, _tenantId, "Final Exam", -1));
        Assert.Equal("passThreshold", exception.ParamName);
    }

    [Fact]
    public void Create_WithPassThresholdOver100_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            Exam.Create(_lessonId, _tenantId, "Final Exam", 101));
        Assert.Equal("passThreshold", exception.ParamName);
    }

    [Fact]
    public void SetTitle_WithValidTitle_UpdatesTitle()
    {
        // Arrange
        var exam = Exam.Create(_lessonId, _tenantId, "Final Exam", 70);

        // Act
        exam.SetTitle("Midterm Exam");

        // Assert
        Assert.Equal("Midterm Exam", exam.Title);
    }

    [Fact]
    public void SetTitle_WithEmptyTitle_ThrowsArgumentException()
    {
        // Arrange
        var exam = Exam.Create(_lessonId, _tenantId, "Final Exam", 70);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => exam.SetTitle(""));
        Assert.Equal("title", exception.ParamName);
    }

    [Fact]
    public void SetTitle_WithTitleTooLong_ThrowsArgumentException()
    {
        // Arrange
        var exam = Exam.Create(_lessonId, _tenantId, "Final Exam", 70);
        var longTitle = new string('a', 201);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => exam.SetTitle(longTitle));
        Assert.Equal("title", exception.ParamName);
    }

    [Fact]
    public void SetPassThreshold_WithValidThreshold_UpdatesThreshold()
    {
        // Arrange
        var exam = Exam.Create(_lessonId, _tenantId, "Final Exam", 70);

        // Act
        exam.SetPassThreshold(80);

        // Assert
        Assert.Equal(80, exam.PassThreshold);
    }

    [Fact]
    public void SetPassThreshold_WithNegativeThreshold_ThrowsArgumentException()
    {
        // Arrange
        var exam = Exam.Create(_lessonId, _tenantId, "Final Exam", 70);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => exam.SetPassThreshold(-1));
        Assert.Equal("passThreshold", exception.ParamName);
    }

    [Fact]
    public void SetPassThreshold_WithThresholdOver100_ThrowsArgumentException()
    {
        // Arrange
        var exam = Exam.Create(_lessonId, _tenantId, "Final Exam", 70);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => exam.SetPassThreshold(101));
        Assert.Equal("passThreshold", exception.ParamName);
    }
}
