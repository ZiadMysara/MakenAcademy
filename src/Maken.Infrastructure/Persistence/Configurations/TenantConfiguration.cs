using Maken.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maken.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework Core configuration for the Tenant entity.
/// </summary>
public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        // Table name
        builder.ToTable("Tenants");

        // Primary key
        builder.HasKey(t => t.Id);

        // Properties
        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.Subdomain)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(t => t.LogoUrl)
            .HasMaxLength(500);

        builder.Property(t => t.PrimaryColor)
            .HasMaxLength(7);

        builder.Property(t => t.SecondaryColor)
            .HasMaxLength(7);

        builder.Property(t => t.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Audit fields (inherited from BaseEntity)
        builder.Property(t => t.CreatedAt)
            .IsRequired();

        builder.Property(t => t.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        // Indexes
        builder.HasIndex(t => t.Subdomain)
            .IsUnique()
            .HasDatabaseName("IX_Tenant_Subdomain");

        builder.HasIndex(t => t.IsDeleted)
            .HasDatabaseName("IX_Tenant_IsDeleted");

        // Relationships
        builder.HasMany(t => t.Users)
            .WithOne(u => u.Tenant)
            .HasForeignKey(u => u.TenantId)
            .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete
    }
}
