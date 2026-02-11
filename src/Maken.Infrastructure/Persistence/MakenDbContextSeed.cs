using Maken.Application.Common.Interfaces;
using Maken.Domain.Entities;
using Maken.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Maken.Infrastructure.Persistence;

/// <summary>
/// Seeds initial data for development and testing.
/// </summary>
public static class MakenDbContextSeed
{
    /// <summary>
    /// Seeds the database with initial tenant and admin user.
    /// </summary>
    public static async Task SeedAsync(MakenDbContext context, IPasswordHasher passwordHasher)
    {
        // Check if data already exists (use IgnoreQueryFilters to bypass tenant filter)
        if (await context.Tenants.IgnoreQueryFilters().AnyAsync())
        {
            return; // Database already seeded
        }

        // Create default tenant
        var tenant = new Tenant("Demo Academy", "demo");
        tenant.SetBranding(
            logoUrl: "https://via.placeholder.com/150",
            primaryColor: "#1E40AF",
            secondaryColor: "#64748B"
        );
        context.Tenants.Add(tenant);
        await context.SaveChangesAsync();

        // Create admin user
        var passwordHash = passwordHasher.HashPassword("Admin123!");
        var admin = new User(
            email: "admin@example.com",
            passwordHash: passwordHash,
            firstName: "Admin",
            lastName: "User",
            role: RoleType.CompanyAdmin,
            tenantId: tenant.Id
        );
        context.Users.Add(admin);

        // Create instructor user
        var instructorPasswordHash = passwordHasher.HashPassword("Instructor123!");
        var instructor = new User(
            email: "instructor@example.com",
            passwordHash: instructorPasswordHash,
            firstName: "John",
            lastName: "Instructor",
            role: RoleType.Instructor,
            tenantId: tenant.Id
        );
        context.Users.Add(instructor);

        // Create student user
        var studentPasswordHash = passwordHasher.HashPassword("Student123!");
        var student = new User(
            email: "student@example.com",
            passwordHash: studentPasswordHash,
            firstName: "Jane",
            lastName: "Student",
            role: RoleType.Student,
            tenantId: tenant.Id
        );
        context.Users.Add(student);

        await context.SaveChangesAsync();
    }
}
