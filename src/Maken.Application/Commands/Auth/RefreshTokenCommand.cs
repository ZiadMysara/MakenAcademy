using MediatR;

namespace Maken.Application.Commands.Auth;

/// <summary>
/// Command to refresh an access token using a valid refresh token.
/// </summary>
public sealed record RefreshTokenCommand(
    string RefreshToken
) : IRequest<RefreshTokenResult>;

/// <summary>
/// Result of a refresh token operation containing new access and refresh tokens.
/// </summary>
public sealed record RefreshTokenResult(
    string AccessToken,
    string RefreshToken
);
