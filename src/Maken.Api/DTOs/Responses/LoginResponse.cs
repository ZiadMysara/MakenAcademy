namespace Maken.Api.DTOs.Responses;

/// <summary>
/// Response DTO for successful login containing JWT tokens and user information.
/// </summary>
public sealed record LoginResponse(
    string AccessToken,
    string RefreshToken,
    Guid UserId,
    string Email,
    string Role
);
