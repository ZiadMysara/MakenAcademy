using Maken.Domain.Common;
using Maken.Domain.Enums;

namespace Maken.Domain.Entities;

/// <summary>
/// Represents a user in the Maken platform.
/// Users belong to one tenant (except PlatformAdmin) and have one role.
/// </summary>
/// <remarks>
/// Constitution requirements:
/// - TenantId is null ONLY for PlatformAdmin role
/// - Email must be unique within a tenant (not globally)
/// - Only one role per user (no multi-role support)
/// </remarks>
public class User : BaseEntity
{
    /// <summary>
    /// The ID of the tenant this user belongs to.
    /// Null only for PlatformAdmin role.
    /// </summary>
    public Guid? TenantId { get; private set; }

    /// <summary>
    /// User's email address (used for login).
    /// Must be unique within a tenant.
    /// </summary>
    public string Email { get; private set; } = string.Empty;

    /// <summary>
    /// Hashed password (bcrypt).
    /// Never store plain-text passwords.
    /// </summary>
    public string PasswordHash { get; private set; } = string.Empty;

    /// <summary>
    /// User's first name.
    /// </summary>
    public string FirstName { get; private set; } = string.Empty;

    /// <summary>
    /// User's last name.
    /// </summary>
    public string LastName { get; private set; } = string.Empty;

    /// <summary>
    /// User's role in the system.
    /// Constitution requirement: Only one role per user.
    /// </summary>
    public RoleType Role { get; private set; }

    /// <summary>
    /// Indicates whether this user account is active.
    /// Inactive users cannot log in.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// UTC timestamp of the user's last successful login (optional).
    /// </summary>
    public DateTime? LastLoginAt { get; private set; }

    /// <summary>
    /// Full name of the user (computed property).
    /// </summary>
    public string FullName => $"{FirstName} {LastName}";

    // Navigation properties
    /// <summary>
    /// The tenant this user belongs to (null for PlatformAdmin).
    /// </summary>
    public Tenant? Tenant { get; private set; }

    /// <summary>
    /// Private constructor for EF Core.
    /// </summary>
    private User() : base()
    {
    }

    /// <summary>
    /// Creates a new user.
    /// </summary>
    /// <param name="email">User's email address.</param>
    /// <param name="passwordHash">Hashed password (bcrypt).</param>
    /// <param name="firstName">User's first name.</param>
    /// <param name="lastName">User's last name.</param>
    /// <param name="role">User's role.</param>
    /// <param name="tenantId">Tenant ID (required except for PlatformAdmin).</param>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid.</exception>
    public User(
        string email,
        string passwordHash,
        string firstName,
        string lastName,
        RoleType role,
        Guid? tenantId = null) : base()
    {
        SetEmail(email);
        SetPasswordHash(passwordHash);
        SetFirstName(firstName);
        SetLastName(lastName);
        SetRole(role, tenantId);
        IsActive = true;
    }

    /// <summary>
    /// Updates the user's email address.
    /// </summary>
    /// <param name="email">New email address (max 256 characters, valid format).</param>
    /// <exception cref="ArgumentException">Thrown when email is invalid.</exception>
    public void SetEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email cannot be empty.", nameof(email));
        }

        email = email.Trim().ToLowerInvariant();

        if (email.Length > 256)
        {
            throw new ArgumentException("Email cannot exceed 256 characters.", nameof(email));
        }

        // Basic email format validation
        if (!System.Text.RegularExpressions.Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        {
            throw new ArgumentException("Email format is invalid.", nameof(email));
        }

        Email = email;
    }

    /// <summary>
    /// Updates the user's password hash.
    /// </summary>
    /// <param name="passwordHash">New password hash (max 256 characters).</param>
    /// <exception cref="ArgumentException">Thrown when password hash is invalid.</exception>
    public void SetPasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException("Password hash cannot be empty.", nameof(passwordHash));
        }

        if (passwordHash.Length > 256)
        {
            throw new ArgumentException("Password hash cannot exceed 256 characters.", nameof(passwordHash));
        }

        PasswordHash = passwordHash;
    }

    /// <summary>
    /// Updates the user's first name.
    /// </summary>
    /// <param name="firstName">New first name (1-100 characters).</param>
    /// <exception cref="ArgumentException">Thrown when first name is invalid.</exception>
    public void SetFirstName(string firstName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new ArgumentException("First name cannot be empty.", nameof(firstName));
        }

        if (firstName.Length > 100)
        {
            throw new ArgumentException("First name cannot exceed 100 characters.", nameof(firstName));
        }

        FirstName = firstName.Trim();
    }

    /// <summary>
    /// Updates the user's last name.
    /// </summary>
    /// <param name="lastName">New last name (1-100 characters).</param>
    /// <exception cref="ArgumentException">Thrown when last name is invalid.</exception>
    public void SetLastName(string lastName)
    {
        if (string.IsNullOrWhiteSpace(lastName))
        {
            throw new ArgumentException("Last name cannot be empty.", nameof(lastName));
        }

        if (lastName.Length > 100)
        {
            throw new ArgumentException("Last name cannot exceed 100 characters.", nameof(lastName));
        }

        LastName = lastName.Trim();
    }

    /// <summary>
    /// Updates the user's role and validates tenant assignment.
    /// Constitution requirement: TenantId is null ONLY for PlatformAdmin.
    /// </summary>
    /// <param name="role">New role.</param>
    /// <param name="tenantId">Tenant ID (required except for PlatformAdmin).</param>
    /// <exception cref="ArgumentException">Thrown when role/tenant combination is invalid.</exception>
    public void SetRole(RoleType role, Guid? tenantId)
    {
        // Constitution rule: PlatformAdmin cannot belong to any tenant
        if (role == RoleType.PlatformAdmin && tenantId.HasValue)
        {
            throw new ArgumentException("PlatformAdmin cannot belong to a tenant.", nameof(tenantId));
        }

        // Constitution rule: All other roles must belong to a tenant
        if (role != RoleType.PlatformAdmin && !tenantId.HasValue)
        {
            throw new ArgumentException($"{role} must belong to a tenant.", nameof(tenantId));
        }

        Role = role;
        TenantId = tenantId;
    }

    /// <summary>
    /// Activates the user account, allowing login.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
    }

    /// <summary>
    /// Deactivates the user account, preventing login.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
    }

    /// <summary>
    /// Records a successful login.
    /// </summary>
    public void RecordLogin()
    {
        LastLoginAt = DateTime.UtcNow;
    }
}
