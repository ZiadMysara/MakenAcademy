using Maken.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maken.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for Lesson entity.
/// </summary>
public sealed class LessonConfiguration : IEntityTypeConfiguration<Lesson>
{
    public void Configure(EntityTypeBuilder<Lesson> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.CourseId).IsRequired();
        builder.Property(l => l.TenantId).IsRequired();
        builder.Property(l => l.Title).IsRequired().HasMaxLength(200);
        builder.Property(l => l.Description).IsRequired().HasMaxLength(2000);
        builder.Property(l => l.ContentType).IsRequired();
        builder.Property(l => l.ContentUrl).IsRequired().HasMaxLength(500);
        builder.Property(l => l.Order).IsRequired();

        // Indexes
        builder.HasIndex(l => l.CourseId);
        builder.HasIndex(l => l.TenantId);
        builder.HasIndex(l => l.IsDeleted);

        // Unique index on CourseId + Order (for non-deleted lessons)
        builder.HasIndex(l => new { l.CourseId, l.Order })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        // Relationships
        builder.HasOne(l => l.Course)
            .WithMany(c => c.Lessons)
            .HasForeignKey(l => l.CourseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.Exam)
            .WithOne(e => e.Lesson)
            .HasForeignKey<Exam>(e => e.LessonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(l => l.ProgressRecords)
            .WithOne(p => p.Lesson)
            .HasForeignKey(p => p.LessonId)
            .OnDelete(DeleteBehavior.Restrict);

        // Query filter for soft delete
        builder.HasQueryFilter(l => !l.IsDeleted);
    }
}
