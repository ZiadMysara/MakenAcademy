using Maken.Domain.Entities;

namespace Maken.Application.Common.Interfaces;

/// <summary>
/// Service for generating and validating JWT tokens.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Generates an access token (JWT) for the specified user.
    /// Token includes claims: UserId, TenantId, Role, Email.
    /// </summary>
    /// <param name="user">The user to generate the token for.</param>
    /// <returns>A JWT access token string.</returns>
    string GenerateAccessToken(User user);

    /// <summary>
    /// Generates a secure random refresh token.
    /// </summary>
    /// <returns>A secure random string to be used as a refresh token.</returns>
    string GenerateRefreshToken();

    /// <summary>
    /// Validates a JWT access token and extracts the user ID.
    /// </summary>
    /// <param name="token">The JWT token to validate.</param>
    /// <returns>The user ID if the token is valid; otherwise, null.</returns>
    Guid? ValidateToken(string token);
}
