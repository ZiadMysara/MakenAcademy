using Maken.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maken.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for Progress entity.
/// </summary>
public class ProgressConfiguration : IEntityTypeConfiguration<Progress>
{
    public void Configure(EntityTypeBuilder<Progress> builder)
    {
        builder.ToTable("Progress");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.StudentId)
            .IsRequired();

        builder.Property(p => p.LessonId)
            .IsRequired();

        builder.Property(p => p.TenantId)
            .IsRequired();

        builder.Property(p => p.CompletedAt)
            .IsRequired(false);

        builder.Property(p => p.ExamPassed)
            .IsRequired(false);

        builder.Property(p => p.ExamScore)
            .IsRequired(false);

        builder.Property(p => p.CreatedAt)
            .IsRequired();

        builder.Property(p => p.UpdatedAt)
            .IsRequired(false);

        // Unique index on StudentId + LessonId (one progress record per student per lesson)
        builder.HasIndex(p => new { p.StudentId, p.LessonId })
            .IsUnique()
            .HasDatabaseName("IX_Progress_StudentId_LessonId");

        // Index on TenantId for tenant isolation queries
        builder.HasIndex(p => p.TenantId)
            .HasDatabaseName("IX_Progress_TenantId");

        // Relationships
        builder.HasOne(p => p.Student)
            .WithMany()
            .HasForeignKey(p => p.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Lesson)
            .WithMany(l => l.ProgressRecords)
            .HasForeignKey(p => p.LessonId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
