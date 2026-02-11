using Maken.Domain.Entities;
using Maken.Domain.Enums;
using Xunit;

namespace Maken.Domain.Tests.Entities;

public class LessonTests
{
    private readonly Guid _courseId = Guid.NewGuid();
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidData_CreatesLesson()
    {
        // Act
        var lesson = Lesson.Create(
            _courseId,
            _tenantId,
            "Introduction to Programming",
            "Learn the basics of programming",
            ContentType.Video,
            "https://example.com/video.mp4",
            1
        );

        // Assert
        Assert.NotNull(lesson);
        Assert.Equal(_courseId, lesson.CourseId);
        Assert.Equal(_tenantId, lesson.TenantId);
        Assert.Equal("Introduction to Programming", lesson.Title);
        Assert.Equal("Learn the basics of programming", lesson.Description);
        Assert.Equal(ContentType.Video, lesson.ContentType);
        Assert.Equal("https://example.com/video.mp4", lesson.ContentUrl);
        Assert.Equal(1, lesson.Order);
    }

    [Fact]
    public void Create_WithEmptyCourseId_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            Lesson.Create(
                Guid.Empty,
                _tenantId,
                "Title",
                "Description",
                ContentType.Video,
                "https://example.com/video.mp4",
                1
            )
        );
    }

    [Fact]
    public void Create_WithEmptyTenantId_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            Lesson.Create(
                _courseId,
                Guid.Empty,
                "Title",
                "Description",
                ContentType.Video,
                "https://example.com/video.mp4",
                1
            )
        );
    }

    [Fact]
    public void Create_WithEmptyTitle_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            Lesson.Create(
                _courseId,
                _tenantId,
                "",
                "Description",
                ContentType.Video,
                "https://example.com/video.mp4",
                1
            )
        );
    }

    [Fact]
    public void Create_WithTitleTooLong_ThrowsArgumentException()
    {
        // Arrange
        var longTitle = new string('a', 201);

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            Lesson.Create(
                _courseId,
                _tenantId,
                longTitle,
                "Description",
                ContentType.Video,
                "https://example.com/video.mp4",
                1
            )
        );
    }

    [Fact]
    public void Create_WithEmptyDescription_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            Lesson.Create(
                _courseId,
                _tenantId,
                "Title",
                "",
                ContentType.Video,
                "https://example.com/video.mp4",
                1
            )
        );
    }

    [Fact]
    public void Create_WithDescriptionTooLong_ThrowsArgumentException()
    {
        // Arrange
        var longDescription = new string('a', 2001);

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            Lesson.Create(
                _courseId,
                _tenantId,
                "Title",
                longDescription,
                ContentType.Video,
                "https://example.com/video.mp4",
                1
            )
        );
    }

    [Fact]
    public void Create_WithEmptyContentUrl_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            Lesson.Create(
                _courseId,
                _tenantId,
                "Title",
                "Description",
                ContentType.Video,
                "",
                1
            )
        );
    }

    [Fact]
    public void Create_WithContentUrlTooLong_ThrowsArgumentException()
    {
        // Arrange
        var longUrl = new string('a', 501);

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            Lesson.Create(
                _courseId,
                _tenantId,
                "Title",
                "Description",
                ContentType.Video,
                longUrl,
                1
            )
        );
    }

    [Fact]
    public void Create_WithInvalidOrder_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            Lesson.Create(
                _courseId,
                _tenantId,
                "Title",
                "Description",
                ContentType.Video,
                "https://example.com/video.mp4",
                0
            )
        );
    }

    [Fact]
    public void SetOrder_WithValidOrder_UpdatesOrder()
    {
        // Arrange
        var lesson = Lesson.Create(
            _courseId,
            _tenantId,
            "Title",
            "Description",
            ContentType.Video,
            "https://example.com/video.mp4",
            1
        );

        // Act
        lesson.SetOrder(2);

        // Assert
        Assert.Equal(2, lesson.Order);
    }

    [Fact]
    public void SetOrder_WithInvalidOrder_ThrowsArgumentException()
    {
        // Arrange
        var lesson = Lesson.Create(
            _courseId,
            _tenantId,
            "Title",
            "Description",
            ContentType.Video,
            "https://example.com/video.mp4",
            1
        );

        // Act & Assert
        Assert.Throws<ArgumentException>(() => lesson.SetOrder(0));
    }

    [Fact]
    public void UpdateContent_WithValidData_UpdatesContent()
    {
        // Arrange
        var lesson = Lesson.Create(
            _courseId,
            _tenantId,
            "Title",
            "Description",
            ContentType.Video,
            "https://example.com/video.mp4",
            1
        );

        // Act
        lesson.UpdateContent(ContentType.PDF, "https://example.com/document.pdf");

        // Assert
        Assert.Equal(ContentType.PDF, lesson.ContentType);
        Assert.Equal("https://example.com/document.pdf", lesson.ContentUrl);
    }

    [Fact]
    public void UpdateContent_WithEmptyUrl_ThrowsArgumentException()
    {
        // Arrange
        var lesson = Lesson.Create(
            _courseId,
            _tenantId,
            "Title",
            "Description",
            ContentType.Video,
            "https://example.com/video.mp4",
            1
        );

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            lesson.UpdateContent(ContentType.PDF, "")
        );
    }

    [Fact]
    public void SetTitle_WithValidTitle_UpdatesTitle()
    {
        // Arrange
        var lesson = Lesson.Create(
            _courseId,
            _tenantId,
            "Original Title",
            "Description",
            ContentType.Video,
            "https://example.com/video.mp4",
            1
        );

        // Act
        lesson.SetTitle("Updated Title");

        // Assert
        Assert.Equal("Updated Title", lesson.Title);
    }

    [Fact]
    public void SetTitle_WithEmptyTitle_ThrowsArgumentException()
    {
        // Arrange
        var lesson = Lesson.Create(
            _courseId,
            _tenantId,
            "Original Title",
            "Description",
            ContentType.Video,
            "https://example.com/video.mp4",
            1
        );

        // Act & Assert
        Assert.Throws<ArgumentException>(() => lesson.SetTitle(""));
    }

    [Fact]
    public void SetDescription_WithValidDescription_UpdatesDescription()
    {
        // Arrange
        var lesson = Lesson.Create(
            _courseId,
            _tenantId,
            "Title",
            "Original Description",
            ContentType.Video,
            "https://example.com/video.mp4",
            1
        );

        // Act
        lesson.SetDescription("Updated Description");

        // Assert
        Assert.Equal("Updated Description", lesson.Description);
    }

    [Fact]
    public void SetDescription_WithEmptyDescription_ThrowsArgumentException()
    {
        // Arrange
        var lesson = Lesson.Create(
            _courseId,
            _tenantId,
            "Title",
            "Original Description",
            ContentType.Video,
            "https://example.com/video.mp4",
            1
        );

        // Act & Assert
        Assert.Throws<ArgumentException>(() => lesson.SetDescription(""));
    }

    [Fact]
    public void Create_TrimsWhitespace_FromStringProperties()
    {
        // Act
        var lesson = Lesson.Create(
            _courseId,
            _tenantId,
            "  Title  ",
            "  Description  ",
            ContentType.Video,
            "  https://example.com/video.mp4  ",
            1
        );

        // Assert
        Assert.Equal("Title", lesson.Title);
        Assert.Equal("Description", lesson.Description);
        Assert.Equal("https://example.com/video.mp4", lesson.ContentUrl);
    }
}
