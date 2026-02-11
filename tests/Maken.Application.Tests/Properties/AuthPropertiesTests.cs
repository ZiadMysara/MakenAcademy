using FsCheck;
using FsCheck.Xunit;
using Maken.Application.Commands.Auth;
using Maken.Application.Common.Interfaces;
using Maken.Application.Tests.Helpers;
using Maken.Domain.Entities;
using Maken.Domain.Enums;
using Moq;
using Microsoft.Extensions.Logging;

namespace Maken.Application.Tests.Properties;

/// <summary>
/// Property-based tests for authentication correctness properties.
/// Tests universal properties that should hold across all valid inputs.
/// </summary>
public sealed class AuthPropertiesTests
{
    /// <summary>
    /// Helper method to clean and validate email prefix
    /// </summary>
    private static bool TryGetValidEmailPrefix(NonEmptyString emailPrefix, out string validPrefix)
    {
        validPrefix = new string(emailPrefix.Get.Where(c => !char.IsControl(c) && !char.IsWhiteSpace(c)).ToArray()).Trim();
        
        // Skip if empty or contains invalid characters
        if (string.IsNullOrWhiteSpace(validPrefix) || validPrefix.Contains('@'))
        {
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// Helper method to clean and validate name strings
    /// </summary>
    private static bool TryGetValidName(NonEmptyString name, out string validName)
    {
        validName = new string(name.Get.Where(c => !char.IsControl(c)).ToArray()).Trim();
        return !string.IsNullOrWhiteSpace(validName);
    }
    /// <summary>
    /// Property 16: Authentication Token Generation
    /// For any valid email/password combination for an active user, login should return both
    /// an access token and a refresh token. For invalid credentials or inactive users,
    /// login should fail with UnauthorizedAccessException.
    /// 
    /// Feature: backend-business-features, Property 16: Authentication Token Generation
    /// Validates: Requirements FR-027
    /// </summary>
    [Property(MaxTest = 100)]
    public bool Property16_AuthenticationTokenGeneration_ValidCredentials_ReturnsTokens(
        NonEmptyString emailPrefix,
        NonEmptyString firstName,
        NonEmptyString lastName,
        Guid tenantId)
    {
        // Validate and clean inputs
        if (!TryGetValidEmailPrefix(emailPrefix, out var emailPrefixTrimmed) ||
            !TryGetValidName(firstName, out var firstNameTrimmed) ||
            !TryGetValidName(lastName, out var lastNameTrimmed))
        {
            return true; // Skip this test case
        }
        
        // Create valid user
        var email = emailPrefixTrimmed + "@example.com";
        var user = new User(email, "hashed-password", firstNameTrimmed, lastNameTrimmed, RoleType.Student, tenantId);

        // Arrange
        var mockUserRepository = new Mock<IRepository<User>>();
        var mockRefreshTokenRepository = new Mock<IRefreshTokenRepository>();
        var mockJwtService = new Mock<IJwtTokenService>();
        var mockPasswordHasher = new Mock<IPasswordHasher>();

        mockUserRepository
            .Setup(r => r.GetFirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<User, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        mockPasswordHasher
            .Setup(p => p.VerifyPassword(It.IsAny<string>(), user.PasswordHash))
            .Returns(true);

        mockJwtService
            .Setup(j => j.GenerateAccessToken(user))
            .Returns("valid-access-token");

        mockJwtService
            .Setup(j => j.GenerateRefreshToken())
            .Returns("valid-refresh-token");

        mockRefreshTokenRepository
            .Setup(r => r.StoreRefreshTokenAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new LoginCommandHandler(
            mockUserRepository.Object,
            mockJwtService.Object,
            mockPasswordHasher.Object,
            mockRefreshTokenRepository.Object,
            new Mock<ILogger<LoginCommandHandler>>().Object
        );

        var command = new LoginCommand(user.Email, "correct-password");

        // Act
        var result = handler.Handle(command, CancellationToken.None).GetAwaiter().GetResult();

        // Assert - Valid credentials should return both tokens
        return !string.IsNullOrEmpty(result.AccessToken) &&
               !string.IsNullOrEmpty(result.RefreshToken) &&
               result.UserId == user.Id &&
               result.Email == user.Email;
    }

    /// <summary>
    /// Property 16: Authentication Token Generation (Invalid Credentials)
    /// For invalid credentials, login should fail.
    /// 
    /// Feature: backend-business-features, Property 16: Authentication Token Generation
    /// Validates: Requirements FR-027
    /// </summary>
    [Property(MaxTest = 100)]
    public bool Property16_AuthenticationTokenGeneration_InvalidCredentials_Fails(
        NonEmptyString emailPrefix,
        NonEmptyString firstName,
        NonEmptyString lastName,
        Guid tenantId)
    {
        // Validate and clean inputs
        if (!TryGetValidEmailPrefix(emailPrefix, out var emailPrefixTrimmed) ||
            !TryGetValidName(firstName, out var firstNameTrimmed) ||
            !TryGetValidName(lastName, out var lastNameTrimmed))
        {
            return true; // Skip this test case
        }
        
        // Create valid user
        var email = emailPrefixTrimmed + "@example.com";
        var user = new User(email, "hashed-password", firstNameTrimmed, lastNameTrimmed, RoleType.Student, tenantId);

        // Arrange
        var mockUserRepository = new Mock<IRepository<User>>();
        var mockRefreshTokenRepository = new Mock<IRefreshTokenRepository>();
        var mockJwtService = new Mock<IJwtTokenService>();
        var mockPasswordHasher = new Mock<IPasswordHasher>();

        mockUserRepository
            .Setup(r => r.GetFirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<User, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        mockPasswordHasher
            .Setup(p => p.VerifyPassword(It.IsAny<string>(), user.PasswordHash))
            .Returns(false); // Invalid password

        var handler = new LoginCommandHandler(
            mockUserRepository.Object,
            mockJwtService.Object,
            mockPasswordHasher.Object,
            mockRefreshTokenRepository.Object,
            new Mock<ILogger<LoginCommandHandler>>().Object
        );

        var command = new LoginCommand(user.Email, "wrong-password");

        // Act & Assert - Invalid credentials should throw
        try
        {
            var result = handler.Handle(command, CancellationToken.None).GetAwaiter().GetResult();
            // If we reach here, the test failed (should have thrown)
            return false;
        }
        catch (AggregateException ex) when (ex.InnerException is UnauthorizedAccessException)
        {
            // Expected exception - test passed
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            // Also expected - test passed
            return true;
        }
        catch
        {
            // Unexpected exception - test failed
            return false;
        }
    }

    /// <summary>
    /// Property 16: Authentication Token Generation (Non-existent User)
    /// For non-existent users, login should fail.
    /// 
    /// Feature: backend-business-features, Property 16: Authentication Token Generation
    /// Validates: Requirements FR-027
    /// </summary>
    [Property(MaxTest = 100)]
    public bool Property16_AuthenticationTokenGeneration_NonExistentUser_Fails(
        NonEmptyString emailPrefix)
    {
        var email = emailPrefix.Get.Trim() + "@example.com";

        // Arrange
        var mockUserRepository = new Mock<IRepository<User>>();
        var mockRefreshTokenRepository = new Mock<IRefreshTokenRepository>();
        var mockJwtService = new Mock<IJwtTokenService>();
        var mockPasswordHasher = new Mock<IPasswordHasher>();

        mockUserRepository
            .Setup(r => r.GetFirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<User, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null); // User not found

        var handler = new LoginCommandHandler(
            mockUserRepository.Object,
            mockJwtService.Object,
            mockPasswordHasher.Object,
            mockRefreshTokenRepository.Object,
            new Mock<ILogger<LoginCommandHandler>>().Object
        );

        var command = new LoginCommand(email, "any-password");

        // Act & Assert - Non-existent user should throw
        try
        {
            var result = handler.Handle(command, CancellationToken.None).GetAwaiter().GetResult();
            // If we reach here, the test failed (should have thrown)
            return false;
        }
        catch (AggregateException ex) when (ex.InnerException is UnauthorizedAccessException)
        {
            // Expected exception - test passed
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            // Also expected - test passed
            return true;
        }
        catch
        {
            // Unexpected exception - test failed
            return false;
        }
    }

    /// <summary>
    /// Property 16: Authentication Token Generation (Inactive User)
    /// For inactive users, login should fail.
    /// 
    /// Feature: backend-business-features, Property 16: Authentication Token Generation
    /// Validates: Requirements FR-027
    /// </summary>
    [Property(MaxTest = 100)]
    public bool Property16_AuthenticationTokenGeneration_InactiveUser_Fails(
        NonEmptyString emailPrefix,
        NonEmptyString firstName,
        NonEmptyString lastName,
        Guid tenantId)
    {
        // Validate and clean inputs
        if (!TryGetValidEmailPrefix(emailPrefix, out var emailPrefixTrimmed) ||
            !TryGetValidName(firstName, out var firstNameTrimmed) ||
            !TryGetValidName(lastName, out var lastNameTrimmed))
        {
            return true; // Skip this test case
        }
        
        // Create inactive user
        var email = emailPrefixTrimmed + "@example.com";
        var user = new User(email, "hashed-password", firstNameTrimmed, lastNameTrimmed, RoleType.Student, tenantId);
        // Mark as deleted to simulate inactive user
        user.SoftDelete();

        // Arrange
        var mockUserRepository = new Mock<IRepository<User>>();
        var mockRefreshTokenRepository = new Mock<IRefreshTokenRepository>();
        var mockJwtService = new Mock<IJwtTokenService>();
        var mockPasswordHasher = new Mock<IPasswordHasher>();

        mockUserRepository
            .Setup(r => r.GetFirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<User, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        mockPasswordHasher
            .Setup(p => p.VerifyPassword(It.IsAny<string>(), user.PasswordHash))
            .Returns(true);

        var handler = new LoginCommandHandler(
            mockUserRepository.Object,
            mockJwtService.Object,
            mockPasswordHasher.Object,
            mockRefreshTokenRepository.Object,
            new Mock<ILogger<LoginCommandHandler>>().Object
        );

        var command = new LoginCommand(user.Email, "correct-password");

        // Act & Assert - Inactive user should throw
        try
        {
            var result = handler.Handle(command, CancellationToken.None).GetAwaiter().GetResult();
            // If we reach here, the test failed (should have thrown)
            return false;
        }
        catch (AggregateException ex) when (ex.InnerException is UnauthorizedAccessException)
        {
            // Expected exception - test passed
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            // Also expected - test passed
            return true;
        }
        catch
        {
            // Unexpected exception - test failed
            return false;
        }
    }

    /// <summary>
    /// Property 17: Refresh Token Validity
    /// For any valid, non-expired refresh token, refreshing should return a new access token
    /// with updated expiration. For invalid or expired refresh tokens, refreshing should fail
    /// with UnauthorizedAccessException.
    /// 
    /// Feature: backend-business-features, Property 17: Refresh Token Validity
    /// Validates: Requirements FR-028
    /// </summary>
    [Property(MaxTest = 100)]
    public bool Property17_RefreshTokenValidity_ValidToken_ReturnsNewAccessToken(
        NonEmptyString emailPrefix,
        NonEmptyString firstName,
        NonEmptyString lastName,
        Guid tenantId)
    {
        // Validate and clean inputs
        if (!TryGetValidEmailPrefix(emailPrefix, out var emailPrefixTrimmed) ||
            !TryGetValidName(firstName, out var firstNameTrimmed) ||
            !TryGetValidName(lastName, out var lastNameTrimmed))
        {
            return true; // Skip this test case
        }
        
        // Create valid user
        var email = emailPrefixTrimmed + "@example.com";
        var user = new User(email, "hashed-password", firstNameTrimmed, lastNameTrimmed, RoleType.Student, tenantId);

        // Arrange
        var mockRefreshTokenRepository = new Mock<IRefreshTokenRepository>();
        var mockUserRepository = new Mock<IRepository<User>>();
        var mockJwtService = new Mock<IJwtTokenService>();

        mockRefreshTokenRepository
            .Setup(r => r.ValidateRefreshTokenAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user.Id);

        mockUserRepository
            .Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        mockJwtService
            .Setup(j => j.GenerateAccessToken(user))
            .Returns("new-access-token");

        mockJwtService
            .Setup(j => j.GenerateRefreshToken())
            .Returns("new-refresh-token");

        mockRefreshTokenRepository
            .Setup(r => r.RevokeRefreshTokenAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        mockRefreshTokenRepository
            .Setup(r => r.StoreRefreshTokenAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new RefreshTokenCommandHandler(
            mockRefreshTokenRepository.Object,
            mockUserRepository.Object,
            mockJwtService.Object,
            new Mock<ILogger<RefreshTokenCommandHandler>>().Object
        );

        var command = new RefreshTokenCommand("valid-refresh-token");

        // Act
        var result = handler.Handle(command, CancellationToken.None).GetAwaiter().GetResult();

        // Assert - Valid token should return new access token
        return !string.IsNullOrEmpty(result.AccessToken) &&
               !string.IsNullOrEmpty(result.RefreshToken);
    }

    /// <summary>
    /// Property 17: Refresh Token Validity (Invalid Token)
    /// For invalid refresh tokens, refreshing should fail.
    /// 
    /// Feature: backend-business-features, Property 17: Refresh Token Validity
    /// Validates: Requirements FR-028
    /// </summary>
    [Property(MaxTest = 100)]
    public bool Property17_RefreshTokenValidity_InvalidToken_Fails(
        NonEmptyString token)
    {
        // Arrange
        var mockRefreshTokenRepository = new Mock<IRefreshTokenRepository>();
        var mockUserRepository = new Mock<IRepository<User>>();
        var mockJwtService = new Mock<IJwtTokenService>();

        mockRefreshTokenRepository
            .Setup(r => r.ValidateRefreshTokenAsync(token.Get.Trim(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid?)null); // Token not valid

        var handler = new RefreshTokenCommandHandler(
            mockRefreshTokenRepository.Object,
            mockUserRepository.Object,
            mockJwtService.Object,
            new Mock<ILogger<RefreshTokenCommandHandler>>().Object
        );

        var tokenTrimmed = token.Get.Trim();
        var command = new RefreshTokenCommand(tokenTrimmed);

        // Act & Assert - Invalid token should throw
        try
        {
            var result = handler.Handle(command, CancellationToken.None).GetAwaiter().GetResult();
            // If we reach here, the test failed (should have thrown)
            return false;
        }
        catch (AggregateException ex) when (ex.InnerException is UnauthorizedAccessException)
        {
            // Expected exception - test passed
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            // Also expected - test passed
            return true;
        }
        catch
        {
            // Unexpected exception - test failed
            return false;
        }
    }
}
