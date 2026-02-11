namespace Maken.Api.DTOs.Responses;

/// <summary>
/// Response DTO for public organization information.
/// </summary>
public class PublicOrganizationResponse
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
    /// Subdomain for tenant access.
    /// </summary>
    public string Subdomain { get; set; } = string.Empty;

    /// <summary>
    /// URL to organization logo (optional).
    /// </summary>
    public string? LogoUrl { get; set; }

    /// <summary>
    /// Primary branding color (hex format).
    /// </summary>
    public string? PrimaryColor { get; set; }

    /// <summary>
    /// Secondary branding color (hex format).
    /// </summary>
    public string? SecondaryColor { get; set; }
}
