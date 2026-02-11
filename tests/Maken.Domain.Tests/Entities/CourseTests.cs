using Maken.Domain.Entities;
using Maken.Domain.Enums;
using Xunit;

namespace Maken.Domain.Tests.Entities;

public class CourseTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public void Constructor_WithValidParameters_CreatesCourse()
    {
        // Arrange
        var name = "Introduction to Programming";
        var description = "Learn the basics of programming";

        // Act
        var course = new Course(_tenantId, name, description);

        // Assert
        Assert.NotEqual(Guid.Empty, course.Id);
        Assert.Equal(_tenantId, course.TenantId);
        Assert.Equal(name, course.Name);
        Assert.Equal(description, course.Description);
        Assert.Equal(CourseStatus.Draft, course.Status);
        Assert.False(course.FreeFlowMode);
        Assert.Empty(course.PrerequisiteCourseIds);
    }

    [Fact]
    public void Constructor_WithFreeFlowMode_EnablesFreeFlow()
    {
        // Arrange & Act
        var course = new Course(_tenantId, "Test Course", "Description", freeFlowMode: true);

        // Assert
        Assert.True(course.FreeFlowMode);
    }

    [Fact]
    public void Constructor_WithPrerequisites_SetsPrerequisites()
    {
        // Arrange
        var prerequisiteIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

        // Act
        var course = new Course(_tenantId, "Test Course", "Description", prerequisiteCourseIds: prerequisiteIds);

        // Assert
        Assert.Equal(2, course.PrerequisiteCourseIds.Count);
        Assert.Contains(prerequisiteIds[0], course.PrerequisiteCourseIds);
        Assert.Contains(prerequisiteIds[1], course.PrerequisiteCourseIds);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_WithInvalidName_ThrowsArgumentException(string invalidName)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            new Course(_tenantId, invalidName, "Valid description"));
    }

    [Fact]
    public void Constructor_WithNameTooLong_ThrowsArgumentException()
    {
        // Arrange
        var longName = new string('a', 201);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            new Course(_tenantId, longName, "Valid description"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_WithInvalidDescription_ThrowsArgumentException(string invalidDescription)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            new Course(_tenantId, "Valid name", invalidDescription));
    }

    [Fact]
    public void Constructor_WithDescriptionTooLong_ThrowsArgumentException()
    {
        // Arrange
        var longDescription = new string('a', 2001);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            new Course(_tenantId, "Valid name", longDescription));
    }

    [Fact]
    public void SetName_WithValidName_UpdatesName()
    {
        // Arrange
        var course = new Course(_tenantId, "Original Name", "Description");
        var newName = "Updated Name";

        // Act
        course.SetName(newName);

        // Assert
        Assert.Equal(newName, course.Name);
    }

    [Fact]
    public void SetName_TrimsWhitespace()
    {
        // Arrange
        var course = new Course(_tenantId, "Original Name", "Description");

        // Act
        course.SetName("  Trimmed Name  ");

        // Assert
        Assert.Equal("Trimmed Name", course.Name);
    }

    [Fact]
    public void SetDescription_WithValidDescription_UpdatesDescription()
    {
        // Arrange
        var course = new Course(_tenantId, "Name", "Original Description");
        var newDescription = "Updated Description";

        // Act
        course.SetDescription(newDescription);

        // Assert
        Assert.Equal(newDescription, course.Description);
    }

    [Fact]
    public void Publish_ChangesStatusToPublished()
    {
        // Arrange
        var course = new Course(_tenantId, "Name", "Description");
        Assert.Equal(CourseStatus.Draft, course.Status);

        // Act
        course.Publish();

        // Assert
        Assert.Equal(CourseStatus.Published, course.Status);
        Assert.True(course.IsPublished());
    }

    [Fact]
    public void Unpublish_ChangesStatusToDraft()
    {
        // Arrange
        var course = new Course(_tenantId, "Name", "Description");
        course.Publish();

        // Act
        course.Unpublish();

        // Assert
        Assert.Equal(CourseStatus.Draft, course.Status);
        Assert.False(course.IsPublished());
    }

    [Fact]
    public void SetFreeFlowMode_EnablesFreeFlow()
    {
        // Arrange
        var course = new Course(_tenantId, "Name", "Description");
        Assert.False(course.FreeFlowMode);

        // Act
        course.SetFreeFlowMode(true);

        // Assert
        Assert.True(course.FreeFlowMode);
    }

    [Fact]
    public void SetFreeFlowMode_DisablesFreeFlow()
    {
        // Arrange
        var course = new Course(_tenantId, "Name", "Description", freeFlowMode: true);

        // Act
        course.SetFreeFlowMode(false);

        // Assert
        Assert.False(course.FreeFlowMode);
    }

    [Fact]
    public void AddPrerequisite_WithValidId_AddsPrerequisite()
    {
        // Arrange
        var course = new Course(_tenantId, "Name", "Description");
        var prerequisiteId = Guid.NewGuid();

        // Act
        course.AddPrerequisite(prerequisiteId);

        // Assert
        Assert.Single(course.PrerequisiteCourseIds);
        Assert.Contains(prerequisiteId, course.PrerequisiteCourseIds);
        Assert.True(course.HasPrerequisites());
    }

    [Fact]
    public void AddPrerequisite_WithEmptyGuid_ThrowsArgumentException()
    {
        // Arrange
        var course = new Course(_tenantId, "Name", "Description");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => course.AddPrerequisite(Guid.Empty));
    }

    [Fact]
    public void AddPrerequisite_WithSelfReference_ThrowsArgumentException()
    {
        // Arrange
        var course = new Course(_tenantId, "Name", "Description");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => course.AddPrerequisite(course.Id));
    }

    [Fact]
    public void AddPrerequisite_WithDuplicateId_ThrowsArgumentException()
    {
        // Arrange
        var course = new Course(_tenantId, "Name", "Description");
        var prerequisiteId = Guid.NewGuid();
        course.AddPrerequisite(prerequisiteId);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => course.AddPrerequisite(prerequisiteId));
    }

    [Fact]
    public void RemovePrerequisite_WithExistingId_RemovesPrerequisite()
    {
        // Arrange
        var course = new Course(_tenantId, "Name", "Description");
        var prerequisiteId = Guid.NewGuid();
        course.AddPrerequisite(prerequisiteId);

        // Act
        course.RemovePrerequisite(prerequisiteId);

        // Assert
        Assert.Empty(course.PrerequisiteCourseIds);
        Assert.False(course.HasPrerequisites());
    }

    [Fact]
    public void RemovePrerequisite_WithNonExistingId_ThrowsArgumentException()
    {
        // Arrange
        var course = new Course(_tenantId, "Name", "Description");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => course.RemovePrerequisite(Guid.NewGuid()));
    }

    [Fact]
    public void ClearPrerequisites_RemovesAllPrerequisites()
    {
        // Arrange
        var course = new Course(_tenantId, "Name", "Description");
        course.AddPrerequisite(Guid.NewGuid());
        course.AddPrerequisite(Guid.NewGuid());
        Assert.Equal(2, course.PrerequisiteCourseIds.Count);

        // Act
        course.ClearPrerequisites();

        // Assert
        Assert.Empty(course.PrerequisiteCourseIds);
        Assert.False(course.HasPrerequisites());
    }

    [Fact]
    public void HasPrerequisites_WithNoPrerequisites_ReturnsFalse()
    {
        // Arrange
        var course = new Course(_tenantId, "Name", "Description");

        // Act & Assert
        Assert.False(course.HasPrerequisites());
    }

    [Fact]
    public void HasPrerequisites_WithPrerequisites_ReturnsTrue()
    {
        // Arrange
        var course = new Course(_tenantId, "Name", "Description");
        course.AddPrerequisite(Guid.NewGuid());

        // Act & Assert
        Assert.True(course.HasPrerequisites());
    }

    [Fact]
    public void IsPublished_WithDraftStatus_ReturnsFalse()
    {
        // Arrange
        var course = new Course(_tenantId, "Name", "Description");

        // Act & Assert
        Assert.False(course.IsPublished());
    }

    [Fact]
    public void IsPublished_WithPublishedStatus_ReturnsTrue()
    {
        // Arrange
        var course = new Course(_tenantId, "Name", "Description");
        course.Publish();

        // Act & Assert
        Assert.True(course.IsPublished());
    }
}
