using Maken.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maken.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework Core configuration for the Enrollment entity.
/// </summary>
public class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> builder)
    {
        builder.ToTable("Enrollments");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.StudentId)
            .IsRequired();

        builder.Property(e => e.CourseId)
            .IsRequired();

        builder.Property(e => e.TenantId)
            .IsRequired();

        builder.Property(e => e.EnrolledAt)
            .IsRequired();

        builder.Property(e => e.CompletedAt)
            .IsRequired(false);

        // Unique index on StudentId + CourseId to prevent duplicate enrollments
        builder.HasIndex(e => new { e.StudentId, e.CourseId })
            .IsUnique()
            .HasDatabaseName("IX_Enrollments_StudentId_CourseId");

        // Index on TenantId for tenant isolation queries
        builder.HasIndex(e => e.TenantId)
            .HasDatabaseName("IX_Enrollments_TenantId");

        // Index on CourseId for querying enrollments by course
        builder.HasIndex(e => e.CourseId)
            .HasDatabaseName("IX_Enrollments_CourseId");

        // Relationships
        builder.HasOne(e => e.Student)
            .WithMany()
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Course)
            .WithMany(c => c.Enrollments)
            .HasForeignKey(e => e.CourseId)
            .OnDelete(DeleteBehavior.Restrict);

        // Soft delete query filter
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
