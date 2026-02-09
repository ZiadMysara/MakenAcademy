namespace Maken.Domain.Enums;

/// <summary>
/// Defines the fixed roles in the Maken platform.
/// Constitution requirement: These roles are defined in §4 and cannot be extended
/// without a Constitution amendment.
/// </summary>
/// <remarks>
/// Role hierarchy (higher value = lower privilege):
/// - PlatformAdmin (0): Global scope, manages all tenants
/// - CompanyAdmin (1): Tenant scope, manages tenant settings/users/courses
/// - Instructor (2): Tenant scope, creates courses and views student progress
/// - Student (3): Tenant scope, consumes courses and takes exams
/// </remarks>
public enum RoleType
{
    /// <summary>
    /// Platform administrator with global access.
    /// Can manage all tenants but does not belong to any tenant (TenantId is null).
    /// </summary>
    PlatformAdmin = 0,

    /// <summary>
    /// Company/Institute administrator with tenant-scoped access.
    /// Can manage their tenant's settings, users, and courses.
    /// </summary>
    CompanyAdmin = 1,

    /// <summary>
    /// Instructor with tenant-scoped access.
    /// Can create and manage courses, view student progress.
    /// </summary>
    Instructor = 2,

    /// <summary>
    /// Student with tenant-scoped access.
    /// Can consume courses and take exams.
    /// </summary>
    Student = 3
}
