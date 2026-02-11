using Maken.Application.Common.Interfaces;
using Maken.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Maken.Application.Commands.Auth;

/// <summary>
/// Handler for LoginCommand that authenticates users and generates JWT tokens.
/// </summary>
public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResult>
{
    private readonly IRepository<User> _userRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        IRepository<User> userRepository,
        IJwtTokenService jwtTokenService,
        IPasswordHasher passwordHasher,
        IRefreshTokenRepository refreshTokenRepository,
        ILogger<LoginCommandHandler> logger)
    {
        _userRepository = userRepository;
        _jwtTokenService = jwtTokenService;
        _passwordHasher = passwordHasher;
        _refreshTokenRepository = refreshTokenRepository;
        _logger = logger;
    }

    public async Task<LoginResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Attempting login for email: {Email}", request.Email);

        // Find user by email (case-insensitive)
        var user = await _userRepository.GetFirstOrDefaultAsync(
            u => u.Email == request.Email.ToLowerInvariant(),
            cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("Login failed: User not found for email: {Email}", request.Email);
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        // Verify password hash using BCrypt
        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Login failed: Invalid password for user: {UserId}", user.Id);
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        // Check if user is active
        if (user.IsDeleted)
        {
            _logger.LogWarning("Login failed: User account is inactive for user: {UserId}", user.Id);
            throw new UnauthorizedAccessException("User account is inactive.");
        }

        // Generate JWT tokens
        var accessToken = _jwtTokenService.GenerateAccessToken(user);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();

        // Store refresh token with 7-day expiration
        var refreshTokenExpiration = DateTime.UtcNow.AddDays(7);
        await _refreshTokenRepository.StoreRefreshTokenAsync(
            user.Id,
            refreshToken,
            refreshTokenExpiration,
            cancellationToken);

        _logger.LogInformation("Login successful for user: {UserId}, Email: {Email}, Role: {Role}", 
            user.Id, user.Email, user.Role);

        return new LoginResult(
            AccessToken: accessToken,
            RefreshToken: refreshToken,
            UserId: user.Id,
            Email: user.Email,
            Role: user.Role.ToString()
        );
    }
}
