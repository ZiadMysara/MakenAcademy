namespace Maken.Application.DTOs;

/// <summary>
/// DTO for public organization information displayed on the landing page.
/// Contains only non-sensitive data safe for public exposure.
/// </summary>
public class PublicOrganizationDto
{
    /// <summary>
    /// Organization unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Organization display name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Subdomain for tenant access (e.g., "alazhar" for alazhar.maken.app).
    /// </summary>
    public string Subdomain { get; set; } = string.Empty;

    /// <summary>
    /// URL to organization logo (optional).
    /// </summary>
    public string? LogoUrl { get; set; }

    /// <summary>
    /// Primary branding color in hex format (e.g., "#1E40AF").
    /// </summary>
    public string? PrimaryColor { get; set; }

    /// <summary>
    /// Secondary branding color in hex format (e.g., "#64748B").
    /// </summary>
    public string? SecondaryColor { get; set; }
}
