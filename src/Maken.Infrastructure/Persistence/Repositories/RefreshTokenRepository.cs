using Maken.Application.Common.Interfaces;
using Maken.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Maken.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository for managing refresh tokens.
/// </summary>
public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly MakenDbContext _context;

    public RefreshTokenRepository(MakenDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc />
    public async Task StoreRefreshTokenAsync(Guid userId, string refreshToken, DateTime expiresAt, CancellationToken cancellationToken = default)
    {
        var token = RefreshToken.Create(userId, refreshToken, expiresAt);
        await _context.Set<RefreshToken>().AddAsync(token, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Guid?> ValidateRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var token = await _context.Set<RefreshToken>()
            .FirstOrDefaultAsync(t => t.Token == refreshToken, cancellationToken);

        if (token == null || !token.IsValid())
            return null;

        return token.UserId;
    }

    /// <inheritdoc />
    public async Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var token = await _context.Set<RefreshToken>()
            .FirstOrDefaultAsync(t => t.Token == refreshToken, cancellationToken);

        if (token != null)
        {
            token.Revoke();
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task RevokeAllUserTokensAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var tokens = await _context.Set<RefreshToken>()
            .Where(t => t.UserId == userId && !t.IsRevoked)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
        {
            token.Revoke();
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
