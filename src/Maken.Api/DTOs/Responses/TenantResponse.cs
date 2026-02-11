namespace Maken.Api.DTOs.Responses;

/// <summary>
/// Response DTO for tenant information.
/// </summary>
public sealed record TenantResponse(
    Guid Id,
    string Name,
    string Subdomain,
    bool IsActive
);
