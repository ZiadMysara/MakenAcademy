using Maken.Domain.Common;

namespace Maken.Domain.Entities;

/// <summary>
/// Represents a refresh token for user authentication.
/// </summary>
public sealed class RefreshToken : BaseEntity
{
    /// <summary>
    /// Gets the user ID associated with this refresh token.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Gets the refresh token string.
    /// </summary>
    public string Token { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the expiration date of the refresh token.
    /// </summary>
    public DateTime ExpiresAt { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the refresh token has been revoked.
    /// </summary>
    public bool IsRevoked { get; private set; }

    /// <summary>
    /// Gets the date when the refresh token was revoked.
    /// </summary>
    public DateTime? RevokedAt { get; private set; }

    /// <summary>
    /// Navigation property to the user.
    /// </summary>
    public User User { get; private set; } = null!;

    // Private constructor for EF Core
    private RefreshToken() { }

    /// <summary>
    /// Creates a new refresh token.
    /// </summary>
    public static RefreshToken Create(Guid userId, string token, DateTime expiresAt)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));

        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Token cannot be null or empty.", nameof(token));

        if (expiresAt <= DateTime.UtcNow)
            throw new ArgumentException("Expiration date must be in the future.", nameof(expiresAt));

        return new RefreshToken
        {
            UserId = userId,
            Token = token,
            ExpiresAt = expiresAt,
            IsRevoked = false
        };
    }

    /// <summary>
    /// Revokes the refresh token.
    /// </summary>
    public void Revoke()
    {
        IsRevoked = true;
        RevokedAt = DateTime.UtcNow;
        SetUpdatedBy(Guid.Empty); // System action
    }

    /// <summary>
    /// Checks if the refresh token is valid (not expired and not revoked).
    /// </summary>
    public bool IsValid()
    {
        return !IsRevoked && ExpiresAt > DateTime.UtcNow && !IsDeleted;
    }
}
