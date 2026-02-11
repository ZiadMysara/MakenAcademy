namespace Maken.Application.Common.Interfaces;

/// <summary>
/// Service for hashing and verifying passwords using BCrypt.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hashes a plain text password using BCrypt.
    /// </summary>
    /// <param name="password">The plain text password to hash.</param>
    /// <returns>The BCrypt hashed password.</returns>
    string HashPassword(string password);

    /// <summary>
    /// Verifies that a plain text password matches a BCrypt hash.
    /// </summary>
    /// <param name="password">The plain text password to verify.</param>
    /// <param name="passwordHash">The BCrypt hash to verify against.</param>
    /// <returns>True if the password matches the hash; otherwise, false.</returns>
    bool VerifyPassword(string password, string passwordHash);
}
