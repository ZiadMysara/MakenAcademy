using Maken.Application.Common.Interfaces;

namespace Maken.Infrastructure.Services;

/// <summary>
/// Service for hashing and verifying passwords using BCrypt.
/// </summary>
public sealed class PasswordHasher : IPasswordHasher
{
    /// <inheritdoc />
    public string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be null or empty.", nameof(password));

        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    /// <inheritdoc />
    public bool VerifyPassword(string password, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(password))
            return false;

        if (string.IsNullOrWhiteSpace(passwordHash))
            return false;

        try
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
        catch
        {
            return false;
        }
    }
}
