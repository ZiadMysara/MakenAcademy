using Maken.Domain.Common;

namespace Maken.Domain.Tests.Common;

/// <summary>
/// Tests for BaseEntity to verify Constitution requirements:
/// - UUID generation on construction
/// - Soft delete behavior
/// - Audit field management
/// </summary>
public class BaseEntityTests
{
    // Test entity for testing BaseEntity behavior
    private class TestEntity : BaseEntity
    {
    }

    [Fact]
    public void Constructor_ShouldGenerateUniqueId()
    {
        // Arrange & Act
        var entity = new TestEntity();

        // Assert
        Assert.NotEqual(Guid.Empty, entity.Id);
    }

    [Fact]
    public void Constructor_ShouldGenerateDifferentIdsForDifferentInstances()
    {
        // Arrange & Act
        var entity1 = new TestEntity();
        var entity2 = new TestEntity();

        // Assert
        Assert.NotEqual(entity1.Id, entity2.Id);
    }

    [Fact]
    public void Constructor_ShouldSetCreatedAtToUtcNow()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;

        // Act
        var entity = new TestEntity();

        // Assert
        var afterCreation = DateTime.UtcNow;
        Assert.InRange(entity.CreatedAt, beforeCreation, afterCreation);
    }

    [Fact]
    public void Constructor_ShouldSetIsDeletedToFalse()
    {
        // Arrange & Act
        var entity = new TestEntity();

        // Assert
        Assert.False(entity.IsDeleted);
    }

    [Fact]
    public void Constructor_ShouldLeaveAuditFieldsNull()
    {
        // Arrange & Act
        var entity = new TestEntity();

        // Assert
        Assert.Null(entity.CreatedBy);
        Assert.Null(entity.UpdatedAt);
        Assert.Null(entity.UpdatedBy);
        Assert.Null(entity.DeletedAt);
        Assert.Null(entity.DeletedBy);
    }

    [Fact]
    public void SetCreatedBy_ShouldSetCreatedByField()
    {
        // Arrange
        var entity = new TestEntity();
        var userId = Guid.NewGuid();

        // Act
        entity.SetCreatedBy(userId);

        // Assert
        Assert.Equal(userId, entity.CreatedBy);
    }

    [Fact]
    public void SetUpdatedBy_ShouldSetUpdatedByAndUpdatedAt()
    {
        // Arrange
        var entity = new TestEntity();
        var userId = Guid.NewGuid();
        var beforeUpdate = DateTime.UtcNow;

        // Act
        entity.SetUpdatedBy(userId);

        // Assert
        var afterUpdate = DateTime.UtcNow;
        Assert.Equal(userId, entity.UpdatedBy);
        Assert.NotNull(entity.UpdatedAt);
        Assert.InRange(entity.UpdatedAt.Value, beforeUpdate, afterUpdate);
    }

    [Fact]
    public void SoftDelete_ShouldSetIsDeletedToTrue()
    {
        // Arrange
        var entity = new TestEntity();

        // Act
        entity.SoftDelete();

        // Assert
        Assert.True(entity.IsDeleted);
    }

    [Fact]
    public void SoftDelete_ShouldSetDeletedAtToUtcNow()
    {
        // Arrange
        var entity = new TestEntity();
        var beforeDelete = DateTime.UtcNow;

        // Act
        entity.SoftDelete();

        // Assert
        var afterDelete = DateTime.UtcNow;
        Assert.NotNull(entity.DeletedAt);
        Assert.InRange(entity.DeletedAt.Value, beforeDelete, afterDelete);
    }

    [Fact]
    public void SoftDelete_WithUserId_ShouldSetDeletedBy()
    {
        // Arrange
        var entity = new TestEntity();
        var userId = Guid.NewGuid();

        // Act
        entity.SoftDelete(userId);

        // Assert
        Assert.Equal(userId, entity.DeletedBy);
    }

    [Fact]
    public void SoftDelete_WithoutUserId_ShouldLeaveDeletedByNull()
    {
        // Arrange
        var entity = new TestEntity();

        // Act
        entity.SoftDelete();

        // Assert
        Assert.Null(entity.DeletedBy);
    }

    [Fact]
    public void Restore_ShouldSetIsDeletedToFalse()
    {
        // Arrange
        var entity = new TestEntity();
        entity.SoftDelete(Guid.NewGuid());

        // Act
        entity.Restore();

        // Assert
        Assert.False(entity.IsDeleted);
    }

    [Fact]
    public void Restore_ShouldClearDeletedAtAndDeletedBy()
    {
        // Arrange
        var entity = new TestEntity();
        entity.SoftDelete(Guid.NewGuid());

        // Act
        entity.Restore();

        // Assert
        Assert.Null(entity.DeletedAt);
        Assert.Null(entity.DeletedBy);
    }
}
