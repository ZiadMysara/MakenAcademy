using Maken.Application.Common.Interfaces;
using Maken.Infrastructure.Persistence;
using Maken.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Maken.Infrastructure;

/// <summary>
/// Dependency injection configuration for the Infrastructure layer.
/// Registers database context, repositories, and infrastructure services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds Infrastructure layer services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register TenantContext (resolves from HttpContext)
        // Note: IHttpContextAccessor must be registered by the API layer
        services.AddScoped<ITenantContext, TenantContext>();

        // Register TenantResolver with memory cache (Phase 4: User Story 2)
        services.AddMemoryCache();
        services.AddScoped<ITenantResolver, TenantResolver>();

        // Register DbContext with PostgreSQL (Supabase)
        string connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<MakenDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(MakenDbContext).Assembly.FullName);
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);
            });

            // Enable sensitive data logging in development (check if section exists)
            IConfigurationSection loggingSection = configuration.GetSection("Logging:EnableSensitiveDataLogging");
            if (loggingSection.Exists() && bool.TryParse(loggingSection.Value, out bool enableSensitiveDataLogging) && enableSensitiveDataLogging)
            {
                options.EnableSensitiveDataLogging();
            }
        });

        // Register Unit of Work
        services.AddScoped<IUnitOfWork, Persistence.UnitOfWork>();

        // Register generic repository
        services.AddScoped(typeof(IRepository<>), typeof(Persistence.Repositories.Repository<>));

        // Register authentication services
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IRefreshTokenRepository, Persistence.Repositories.RefreshTokenRepository>();

        // Register repositories
        services.AddScoped<Application.Common.Interfaces.ICourseRepository, Persistence.Repositories.CourseRepository>();
        services.AddScoped<Application.Common.Interfaces.IEnrollmentRepository, Persistence.Repositories.EnrollmentRepository>();
        services.AddScoped<Application.Common.Interfaces.IProgressRepository, Persistence.Repositories.ProgressRepository>();
        services.AddScoped<Application.Common.Interfaces.IExamRepository, Persistence.Repositories.ExamRepository>();

        // Register domain services
        services.AddScoped<Domain.Services.ProgressionService>();
        services.AddScoped<Domain.Services.ExamGradingService>();

        return services;
    }
}
