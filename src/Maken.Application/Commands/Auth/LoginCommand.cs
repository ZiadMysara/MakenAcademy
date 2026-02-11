using MediatR;

namespace Maken.Application.Commands.Auth;

/// <summary>
/// Command to authenticate a user and generate JWT tokens.
/// </summary>
public sealed record LoginCommand(
    string Email,
    string Password
) : IRequest<LoginResult>;

/// <summary>
/// Result of a login operation containing access and refresh tokens.
/// </summary>
public sealed record LoginResult(
    string AccessToken,
    string RefreshToken,
    Guid UserId,
    string Email,
    string Role
);
