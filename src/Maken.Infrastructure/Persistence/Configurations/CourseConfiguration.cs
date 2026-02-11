using Maken.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maken.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework Core configuration for the Course entity.
/// </summary>
public class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        // Table name
        builder.ToTable("Courses");

        // Primary key
        builder.HasKey(c => c.Id);

        // Properties
        builder.Property(c => c.TenantId)
            .IsRequired();

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Description)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(c => c.Status)
            .IsRequired()
            .HasConversion<int>(); // Store enum as int (0=Draft, 1=Published)

        builder.Property(c => c.FreeFlowMode)
            .IsRequired()
            .HasDefaultValue(false);

        // Store PrerequisiteCourseIds as JSON
        builder.Property(c => c.PrerequisiteCourseIds)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<Guid>())
            .HasColumnType("jsonb") // PostgreSQL JSONB type for better performance
            .IsRequired();

        // Audit fields (inherited from BaseEntity)
        builder.Property(c => c.CreatedAt)
            .IsRequired();

        builder.Property(c => c.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        // Indexes
        builder.HasIndex(c => c.TenantId)
            .HasDatabaseName("IX_Course_TenantId");

        builder.HasIndex(c => c.IsDeleted)
            .HasDatabaseName("IX_Course_IsDeleted");

        builder.HasIndex(c => c.Status)
            .HasDatabaseName("IX_Course_Status");

        // Composite index for tenant + status queries
        builder.HasIndex(c => new { c.TenantId, c.Status, c.IsDeleted })
            .HasDatabaseName("IX_Course_TenantId_Status_IsDeleted");

        // Relationships
        builder.HasMany(c => c.Lessons)
            .WithOne(l => l.Course)
            .HasForeignKey(l => l.CourseId)
            .OnDelete(DeleteBehavior.Restrict); // Soft delete handled in application layer

        builder.HasMany(c => c.Enrollments)
            .WithOne(e => e.Course)
            .HasForeignKey(e => e.CourseId)
            .OnDelete(DeleteBehavior.Restrict); // Soft delete handled in application layer
    }
}
