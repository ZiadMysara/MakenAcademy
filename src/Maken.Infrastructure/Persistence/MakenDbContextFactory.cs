using Maken.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Maken.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for creating MakenDbContext instances during migrations.
/// </summary>
public sealed class MakenDbContextFactory : IDesignTimeDbContextFactory<MakenDbContext>
{
    public MakenDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MakenDbContext>();

        // Use local Supabase for migrations
        optionsBuilder.UseNpgsql(
            "Host=127.0.0.1;Port=54322;Database=postgres;Username=postgres;Password=postgres",
            b => b.MigrationsAssembly("Maken.Infrastructure")
        );

        // Pass null for ITenantContext during design-time (migrations)
        return new MakenDbContext(optionsBuilder.Options, null!);
    }
}
