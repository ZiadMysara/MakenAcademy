using Maken.Application.Common.Interfaces;
using Maken.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Maken.Application.Commands.Auth;

/// <summary>
/// Handler for RefreshTokenCommand that validates refresh tokens and generates new access tokens.
/// </summary>
public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, RefreshTokenResult>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IRepository<User> _userRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    public RefreshTokenCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IRepository<User> userRepository,
        IJwtTokenService jwtTokenService,
        ILogger<RefreshTokenCommandHandler> logger)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _userRepository = userRepository;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    public async Task<RefreshTokenResult> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Attempting to refresh token");

        // Validate the refresh token and get the user ID
        var userId = await _refreshTokenRepository.ValidateRefreshTokenAsync(
            request.RefreshToken,
            cancellationToken);

        if (userId == null)
        {
            _logger.LogWarning("Refresh token validation failed: Invalid or expired token");
            throw new UnauthorizedAccessException("Invalid or expired refresh token.");
        }

        // Get the user
        var user = await _userRepository.GetByIdAsync(userId.Value, cancellationToken);

        if (user == null || user.IsDeleted)
        {
            _logger.LogWarning("Refresh token validation failed: User {UserId} not found or inactive", userId.Value);
            throw new UnauthorizedAccessException("User not found or inactive.");
        }

        // Revoke the old refresh token
        await _refreshTokenRepository.RevokeRefreshTokenAsync(
            request.RefreshToken,
            cancellationToken);

        // Generate new tokens
        var newAccessToken = _jwtTokenService.GenerateAccessToken(user);
        var newRefreshToken = _jwtTokenService.GenerateRefreshToken();

        // Store the new refresh token with 7-day expiration
        var refreshTokenExpiration = DateTime.UtcNow.AddDays(7);
        await _refreshTokenRepository.StoreRefreshTokenAsync(
            user.Id,
            newRefreshToken,
            refreshTokenExpiration,
            cancellationToken);

        _logger.LogInformation("Token refresh successful for user: {UserId}", user.Id);

        return new RefreshTokenResult(
            AccessToken: newAccessToken,
            RefreshToken: newRefreshToken
        );
    }
}
