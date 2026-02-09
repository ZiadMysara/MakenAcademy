using Maken.Domain.Common;

namespace Maken.Domain.Entities;

/// <summary>
/// Represents an Islamic Science Institute (Company) in the Maken platform.
/// Each tenant is fully isolated with its own users, courses, and data.
/// </summary>
/// <remarks>
/// Constitution requirement: Tenant isolation is absolute - no cross-tenant data access.
/// Subdomain is used for tenant resolution (e.g., academy.maken.app).
/// </remarks>
public class Tenant : BaseEntity
{
    /// <summary>
    /// Display name of the tenant (e.g., "Al-Azhar Academy").
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// URL subdomain for this tenant (e.g., "academy" for academy.maken.app).
    /// Must be unique, lowercase alphanumeric with optional hyphens.
    /// Cannot be reserved words: www, api, admin, app.
    /// </summary>
    public string Subdomain { get; private set; } = string.Empty;

    /// <summary>
    /// URL to the tenant's logo for branding (optional).
    /// </summary>
    public string? LogoUrl { get; private set; }

    /// <summary>
    /// Primary color for tenant theming in hex format (e.g., "#1E40AF").
    /// </summary>
    public string? PrimaryColor { get; private set; }

    /// <summary>
    /// Secondary color for tenant theming in hex format (e.g., "#64748B").
    /// </summary>
    public string? SecondaryColor { get; private set; }

    /// <summary>
    /// Indicates whether this tenant is active.
    /// Inactive tenants cannot have users log in, but data is preserved.
    /// </summary>
    public bool IsActive { get; private set; }

    // Navigation properties
    /// <summary>
    /// Users belonging to this tenant.
    /// </summary>
    public ICollection<User> Users { get; private set; } = new List<User>();

    /// <summary>
    /// Private constructor for EF Core.
    /// </summary>
    private Tenant() : base()
    {
    }

    /// <summary>
    /// Creates a new tenant.
    /// </summary>
    /// <param name="name">Display name of the tenant.</param>
    /// <param name="subdomain">URL subdomain (must be unique).</param>
    /// <exception cref="ArgumentException">Thrown when name or subdomain is invalid.</exception>
    public Tenant(string name, string subdomain) : base()
    {
        SetName(name);
        SetSubdomain(subdomain);
        IsActive = true;
    }

    /// <summary>
    /// Updates the tenant's display name.
    /// </summary>
    /// <param name="name">New display name (1-200 characters).</param>
    /// <exception cref="ArgumentException">Thrown when name is invalid.</exception>
    public void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Tenant name cannot be empty.", nameof(name));
        }

        if (name.Length > 200)
        {
            throw new ArgumentException("Tenant name cannot exceed 200 characters.", nameof(name));
        }

        Name = name.Trim();
    }

    /// <summary>
    /// Updates the tenant's subdomain.
    /// </summary>
    /// <param name="subdomain">New subdomain (1-50 characters, lowercase alphanumeric + hyphen).</param>
    /// <exception cref="ArgumentException">Thrown when subdomain is invalid or reserved.</exception>
    public void SetSubdomain(string subdomain)
    {
        if (string.IsNullOrWhiteSpace(subdomain))
        {
            throw new ArgumentException("Subdomain cannot be empty.", nameof(subdomain));
        }

        subdomain = subdomain.Trim().ToLowerInvariant();

        if (subdomain.Length > 50)
        {
            throw new ArgumentException("Subdomain cannot exceed 50 characters.", nameof(subdomain));
        }

        // Check for reserved subdomains
        var reservedSubdomains = new[] { "www", "api", "admin", "app" };
        if (reservedSubdomains.Contains(subdomain))
        {
            throw new ArgumentException($"Subdomain '{subdomain}' is reserved and cannot be used.", nameof(subdomain));
        }

        // Validate format: lowercase alphanumeric with optional hyphens
        if (!System.Text.RegularExpressions.Regex.IsMatch(subdomain, @"^[a-z0-9]+(-[a-z0-9]+)*$"))
        {
            throw new ArgumentException("Subdomain must be lowercase alphanumeric with optional hyphens.", nameof(subdomain));
        }

        Subdomain = subdomain;
    }

    /// <summary>
    /// Updates the tenant's branding settings.
    /// </summary>
    /// <param name="logoUrl">URL to logo image (optional, max 500 characters).</param>
    /// <param name="primaryColor">Primary color in hex format (optional, max 7 characters).</param>
    /// <param name="secondaryColor">Secondary color in hex format (optional, max 7 characters).</param>
    public void SetBranding(string? logoUrl = null, string? primaryColor = null, string? secondaryColor = null)
    {
        if (logoUrl != null && logoUrl.Length > 500)
        {
            throw new ArgumentException("Logo URL cannot exceed 500 characters.", nameof(logoUrl));
        }

        if (primaryColor != null && primaryColor.Length > 7)
        {
            throw new ArgumentException("Primary color cannot exceed 7 characters.", nameof(primaryColor));
        }

        if (secondaryColor != null && secondaryColor.Length > 7)
        {
            throw new ArgumentException("Secondary color cannot exceed 7 characters.", nameof(secondaryColor));
        }

        LogoUrl = logoUrl;
        PrimaryColor = primaryColor;
        SecondaryColor = secondaryColor;
    }

    /// <summary>
    /// Activates the tenant, allowing users to log in.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
    }

    /// <summary>
    /// Deactivates the tenant, preventing users from logging in.
    /// Data is preserved and can be reactivated later.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
    }
}
