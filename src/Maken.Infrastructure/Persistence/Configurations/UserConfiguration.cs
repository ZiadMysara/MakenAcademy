using Maken.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maken.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework Core configuration for the User entity.
/// </summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // Table name
        builder.ToTable("Users");

        // Primary key
        builder.HasKey(u => u.Id);

        // Properties
        builder.Property(u => u.TenantId)
            .IsRequired(false); // Nullable for PlatformAdmin

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(u => u.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.Role)
            .IsRequired()
            .HasConversion<int>(); // Store enum as int

        builder.Property(u => u.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Audit fields (inherited from BaseEntity)
        builder.Property(u => u.CreatedAt)
            .IsRequired();

        builder.Property(u => u.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        // Computed property (not mapped to database)
        builder.Ignore(u => u.FullName);

        // Indexes
        builder.HasIndex(u => new { u.Email, u.TenantId })
            .IsUnique()
            .HasDatabaseName("IX_User_Email_TenantId");

        builder.HasIndex(u => u.TenantId)
            .HasDatabaseName("IX_User_TenantId");

        builder.HasIndex(u => u.IsDeleted)
            .HasDatabaseName("IX_User_IsDeleted");

        // Relationships
        builder.HasOne(u => u.Tenant)
            .WithMany(t => t.Users)
            .HasForeignKey(u => u.TenantId)
            .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete
    }
}
