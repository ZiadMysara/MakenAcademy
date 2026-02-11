using Maken.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maken.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core entity configuration for Choice entity.
/// </summary>
public class ChoiceConfiguration : IEntityTypeConfiguration<Choice>
{
    public void Configure(EntityTypeBuilder<Choice> builder)
    {
        // Table name
        builder.ToTable("Choices");

        // Primary key
        builder.HasKey(c => c.Id);

        // Properties
        builder.Property(c => c.Id)
            .IsRequired()
            .ValueGeneratedNever(); // UUID generated in domain

        builder.Property(c => c.QuestionId)
            .IsRequired();

        builder.Property(c => c.Text)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(c => c.IsCorrect)
            .IsRequired();

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        builder.Property(c => c.UpdatedAt)
            .IsRequired(false);

        builder.Property(c => c.DeletedAt)
            .IsRequired(false);

        // Indexes
        builder.HasIndex(c => c.QuestionId)
            .HasDatabaseName("IX_Choices_QuestionId");

        builder.HasIndex(c => c.DeletedAt)
            .HasDatabaseName("IX_Choices_DeletedAt");

        // Relationships
        builder.HasOne<Question>()
            .WithMany()
            .HasForeignKey(c => c.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Global query filter for soft delete
        builder.HasQueryFilter(c => c.DeletedAt == null);
    }
}
