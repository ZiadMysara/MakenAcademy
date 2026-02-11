using Maken.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maken.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core entity configuration for Question entity.
/// </summary>
public class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    public void Configure(EntityTypeBuilder<Question> builder)
    {
        // Table name
        builder.ToTable("Questions");

        // Primary key
        builder.HasKey(q => q.Id);

        // Properties
        builder.Property(q => q.Id)
            .IsRequired()
            .ValueGeneratedNever(); // UUID generated in domain

        builder.Property(q => q.ExamId)
            .IsRequired();

        builder.Property(q => q.Text)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(q => q.Order)
            .IsRequired();

        builder.Property(q => q.CreatedAt)
            .IsRequired();

        builder.Property(q => q.UpdatedAt)
            .IsRequired(false);

        builder.Property(q => q.DeletedAt)
            .IsRequired(false);

        // Indexes
        builder.HasIndex(q => q.ExamId)
            .HasDatabaseName("IX_Questions_ExamId");

        builder.HasIndex(q => q.DeletedAt)
            .HasDatabaseName("IX_Questions_DeletedAt");

        // Relationships
        builder.HasOne(q => q.Exam)
            .WithMany(e => e.Questions)
            .HasForeignKey(q => q.ExamId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(q => q.Choices)
            .WithOne(c => c.Question)
            .HasForeignKey(c => c.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Global query filter for soft delete
        builder.HasQueryFilter(q => q.DeletedAt == null);
    }
}
