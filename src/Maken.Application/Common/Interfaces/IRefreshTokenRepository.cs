namespace Maken.Application.Common.Interfaces;

/// <summary>
/// Repository for managing refresh tokens.
/// </summary>
public interface IRefreshTokenRepository
{
    /// <summary>
    /// Stores a refresh token for a user with an expiration date.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="refreshToken">The refresh token string.</param>
    /// <param name="expiresAt">The expiration date of the refresh token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task StoreRefreshTokenAsync(Guid userId, string refreshToken, DateTime expiresAt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a refresh token and returns the associated user ID if valid.
    /// </summary>
    /// <param name="refreshToken">The refresh token to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user ID if the token is valid and not expired; otherwise, null.</returns>
    Task<Guid?> ValidateRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes a refresh token, making it invalid for future use.
    /// </summary>
    /// <param name="refreshToken">The refresh token to revoke.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes all refresh tokens for a specific user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RevokeAllUserTokensAsync(Guid userId, CancellationToken cancellationToken = default);
}
