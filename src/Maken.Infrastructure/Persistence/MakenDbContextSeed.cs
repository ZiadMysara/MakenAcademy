using Maken.Domain.Entities;
using Maken.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Maken.Infrastructure.Persistence;

/// <summary>
/// Provides seed data for the Maken database.
/// </summary>
public static class MakenDbContextSeed
{
    /// <summary>
    /// Seeds the database with initial demo data.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="logger">The logger instance.</param>
    public static async Task SeedAsync(MakenDbContext context, ILogger logger)
    {
        try
        {
            // Ensure database is created
            await context.Database.EnsureCreatedAsync();

            // Check if data already exists
            if (await context.Tenants.AnyAsync())
            {
                logger.LogInformation("Database already contains data. Skipping seed.");
                return;
            }

            logger.LogInformation("Seeding database with demo data...");

            // Create demo tenant
            var demoTenant = new Tenant("Demo Academy", "demo");
            context.Tenants.Add(demoTenant);

            // Create PlatformAdmin user (no tenant)
            var platformAdmin = new User(
                email: "admin@maken.app",
                passwordHash: "$2a$11$placeholder", // Placeholder - actual auth via Supabase
                firstName: "Platform",
                lastName: "Administrator",
                role: RoleType.PlatformAdmin,
                tenantId: null
            );
            context.Users.Add(platformAdmin);

            // Create CompanyAdmin for demo tenant
            var companyAdmin = new User(
                email: "admin@demo.maken.app",
                passwordHash: "$2a$11$placeholder",
                firstName: "Demo",
                lastName: "Admin",
                role: RoleType.CompanyAdmin,
                tenantId: demoTenant.Id
            );
            context.Users.Add(companyAdmin);

            // Create Instructor for demo tenant
            var instructor = new User(
                email: "instructor@demo.maken.app",
                passwordHash: "$2a$11$placeholder",
                firstName: "Demo",
                lastName: "Instructor",
                role: RoleType.Instructor,
                tenantId: demoTenant.Id
            );
            context.Users.Add(instructor);

            // Create Student for demo tenant
            var student = new User(
                email: "student@demo.maken.app",
                passwordHash: "$2a$11$placeholder",
                firstName: "Demo",
                lastName: "Student",
                role: RoleType.Student,
                tenantId: demoTenant.Id
            );
            context.Users.Add(student);

            await context.SaveChangesAsync();

            logger.LogInformation("Database seeded successfully with demo data.");
            logger.LogInformation("Demo Tenant: {Subdomain} (ID: {TenantId})", demoTenant.Subdomain, demoTenant.Id);
            logger.LogInformation("PlatformAdmin: {Email} ({FullName})", platformAdmin.Email, platformAdmin.FullName);
            logger.LogInformation("CompanyAdmin: {Email} ({FullName})", companyAdmin.Email, companyAdmin.FullName);
            logger.LogInformation("Instructor: {Email} ({FullName})", instructor.Email, instructor.FullName);
            logger.LogInformation("Student: {Email} ({FullName})", student.Email, student.FullName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }
}
