using Maken.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maken.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework Core configuration for the ContactInquiry entity.
/// </summary>
public class ContactInquiryConfiguration : IEntityTypeConfiguration<ContactInquiry>
{
    public void Configure(EntityTypeBuilder<ContactInquiry> builder)
    {
        // Table name
        builder.ToTable("ContactInquiries");

        // Primary key
        builder.HasKey(ci => ci.Id);

        // Properties
        builder.Property(ci => ci.ContactName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(ci => ci.Email)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(ci => ci.OrganizationName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(ci => ci.Message)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(ci => ci.Status)
            .IsRequired()
            .HasDefaultValue(ContactInquiryStatus.New);

        builder.Property(ci => ci.SubmittedAt)
            .IsRequired();

        builder.Property(ci => ci.ReviewedAt)
            .IsRequired(false);

        builder.Property(ci => ci.ReviewedBy)
            .IsRequired(false);

        builder.Property(ci => ci.Notes)
            .HasMaxLength(1000)
            .IsRequired(false);

        // Audit fields (inherited from BaseEntity)
        builder.Property(ci => ci.CreatedAt)
            .IsRequired();

        builder.Property(ci => ci.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        // Indexes for query performance
        builder.HasIndex(ci => ci.Status)
            .HasDatabaseName("IX_ContactInquiry_Status");

        builder.HasIndex(ci => ci.SubmittedAt)
            .HasDatabaseName("IX_ContactInquiry_SubmittedAt");

        builder.HasIndex(ci => ci.IsDeleted)
            .HasDatabaseName("IX_ContactInquiry_IsDeleted");

        // Composite index for admin dashboard queries (IsDeleted, Status, SubmittedAt)
        builder.HasIndex(ci => new { ci.IsDeleted, ci.Status, ci.SubmittedAt })
            .HasDatabaseName("IX_ContactInquiry_IsDeleted_Status_SubmittedAt");

        // Foreign key to User (ReviewedBy) - optional relationship
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(ci => ci.ReviewedBy)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);
    }
}
