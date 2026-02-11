using Maken.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maken.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core entity configuration for Exam entity.
/// </summary>
public class ExamConfiguration : IEntityTypeConfiguration<Exam>
{
    public void Configure(EntityTypeBuilder<Exam> builder)
    {
        // Table name
        builder.ToTable("Exams");

        // Primary key
        builder.HasKey(e => e.Id);

        // Properties
        builder.Property(e => e.Id)
            .IsRequired()
            .ValueGeneratedNever(); // UUID generated in domain

        builder.Property(e => e.LessonId)
            .IsRequired();

        builder.Property(e => e.TenantId)
            .IsRequired();

        builder.Property(e => e.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.PassThreshold)
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .IsRequired(false);

        builder.Property(e => e.DeletedAt)
            .IsRequired(false);

        // Indexes
        builder.HasIndex(e => e.TenantId)
            .HasDatabaseName("IX_Exams_TenantId");

        builder.HasIndex(e => e.LessonId)
            .IsUnique()
            .HasDatabaseName("IX_Exams_LessonId");

        builder.HasIndex(e => e.DeletedAt)
            .HasDatabaseName("IX_Exams_DeletedAt");

        // Relationships
        builder.HasOne<Lesson>()
            .WithMany()
            .HasForeignKey(e => e.LessonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Questions)
            .WithOne(q => q.Exam)
            .HasForeignKey(q => q.ExamId)
            .OnDelete(DeleteBehavior.Cascade);

        // Global query filter for soft delete
        builder.HasQueryFilter(e => e.DeletedAt == null);
    }
}
