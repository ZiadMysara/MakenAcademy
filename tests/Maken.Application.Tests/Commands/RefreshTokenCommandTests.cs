using FluentAssertions;
using Maken.Application.Commands.Auth;
using Maken.Application.Common.Interfaces;
using Maken.Domain.Entities;
using Maken.Domain.Enums;
using Moq;
using Microsoft.Extensions.Logging;

namespace Maken.Application.Tests.Commands;

/// <summary>
/// Unit tests for RefreshTokenCommandHandler.
/// Validates refresh token validation, token rotation, and new token generation.
/// </summary>
public class RefreshTokenCommandTests
{
    private readonly Mock<IRefreshTokenRepository> _mockRefreshTokenRepository;
    private readonly Mock<IRepository<User>> _mockUserRepository;
    private readonly Mock<IJwtTokenService> _mockJwtTokenService;
    private readonly RefreshTokenCommandHandler _handler;

    public RefreshTokenCommandTests()
    {
        _mockRefreshTokenRepository = new Mock<IRefreshTokenRepository>();
        _mockUserRepository = new Mock<IRepository<User>>();
        _mockJwtTokenService = new Mock<IJwtTokenService>();

        _handler = new RefreshTokenCommandHandler(
            _mockRefreshTokenRepository.Object,
            _mockUserRepository.Object,
            _mockJwtTokenService.Object,
            new Mock<ILogger<RefreshTokenCommandHandler>>().Object);
    }

    [Fact]
    public async Task Handle_ValidRefreshToken_ShouldReturnNewTokens()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var email = "test@example.com";
        var passwordHash = "hashedPassword";
        var oldRefreshToken = "old_refresh_token";
        var newAccessToken = "new_access_token";
        var newRefreshToken = "new_refresh_token";

        var user = new User(email, passwordHash, "John", "Doe", RoleType.Student, tenantId);
        var userId = user.Id;

        _mockRefreshTokenRepository
            .Setup(x => x.ValidateRefreshTokenAsync(oldRefreshToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userId);

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockRefreshTokenRepository
            .Setup(x => x.RevokeRefreshTokenAsync(oldRefreshToken, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockJwtTokenService
            .Setup(x => x.GenerateAccessToken(user))
            .Returns(newAccessToken);

        _mockJwtTokenService
            .Setup(x => x.GenerateRefreshToken())
            .Returns(newRefreshToken);

        _mockRefreshTokenRepository
            .Setup(x => x.StoreRefreshTokenAsync(
                userId,
                newRefreshToken,
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var command = new RefreshTokenCommand(oldRefreshToken);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be(newAccessToken);
        result.RefreshToken.Should().Be(newRefreshToken);

        // Verify old token was revoked
        _mockRefreshTokenRepository.Verify(
            x => x.RevokeRefreshTokenAsync(oldRefreshToken, It.IsAny<CancellationToken>()),
            Times.Once);

        // Verify new token was stored with 7-day expiration
        _mockRefreshTokenRepository.Verify(
            x => x.StoreRefreshTokenAsync(
                userId,
                newRefreshToken,
                It.Is<DateTime>(dt => dt > DateTime.UtcNow.AddDays(6) && dt <= DateTime.UtcNow.AddDays(7)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidRefreshToken_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var invalidRefreshToken = "invalid_refresh_token";

        _mockRefreshTokenRepository
            .Setup(x => x.ValidateRefreshTokenAsync(invalidRefreshToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid?)null);

        var command = new RefreshTokenCommand(invalidRefreshToken);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Be("Invalid or expired refresh token.");

        // Verify user lookup was not attempted
        _mockUserRepository.Verify(
            x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);

        // Verify old token was not revoked
        _mockRefreshTokenRepository.Verify(
            x => x.RevokeRefreshTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_UserNotFound_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var refreshToken = "valid_refresh_token";

        _mockRefreshTokenRepository
            .Setup(x => x.ValidateRefreshTokenAsync(refreshToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userId);

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var command = new RefreshTokenCommand(refreshToken);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Be("User not found or inactive.");

        // Verify old token was not revoked
        _mockRefreshTokenRepository.Verify(
            x => x.RevokeRefreshTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);

        // Verify new tokens were not generated
        _mockJwtTokenService.Verify(
            x => x.GenerateAccessToken(It.IsAny<User>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_DeletedUser_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var email = "test@example.com";
        var passwordHash = "hashedPassword";
        var refreshToken = "valid_refresh_token";

        var user = new User(email, passwordHash, "John", "Doe", RoleType.Student, tenantId);
        var userId = user.Id;
        user.SoftDelete(); // Soft delete the user

        _mockRefreshTokenRepository
            .Setup(x => x.ValidateRefreshTokenAsync(refreshToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userId);

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var command = new RefreshTokenCommand(refreshToken);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Be("User not found or inactive.");

        // Verify old token was not revoked
        _mockRefreshTokenRepository.Verify(
            x => x.RevokeRefreshTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);

        // Verify new tokens were not generated
        _mockJwtTokenService.Verify(
            x => x.GenerateAccessToken(It.IsAny<User>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_TokenRotation_ShouldRevokeOldTokenBeforeStoringNew()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var email = "test@example.com";
        var passwordHash = "hashedPassword";
        var oldRefreshToken = "old_refresh_token";
        var newAccessToken = "new_access_token";
        var newRefreshToken = "new_refresh_token";

        var user = new User(email, passwordHash, "John", "Doe", RoleType.Student, tenantId);
        var userId = user.Id;

        var callOrder = new List<string>();

        _mockRefreshTokenRepository
            .Setup(x => x.ValidateRefreshTokenAsync(oldRefreshToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userId);

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockRefreshTokenRepository
            .Setup(x => x.RevokeRefreshTokenAsync(oldRefreshToken, It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("Revoke"))
            .Returns(Task.CompletedTask);

        _mockJwtTokenService
            .Setup(x => x.GenerateAccessToken(user))
            .Returns(newAccessToken);

        _mockJwtTokenService
            .Setup(x => x.GenerateRefreshToken())
            .Returns(newRefreshToken);

        _mockRefreshTokenRepository
            .Setup(x => x.StoreRefreshTokenAsync(
                userId,
                newRefreshToken,
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("Store"))
            .Returns(Task.CompletedTask);

        var command = new RefreshTokenCommand(oldRefreshToken);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert - Verify revoke happens before store
        callOrder.Should().HaveCount(2);
        callOrder[0].Should().Be("Revoke");
        callOrder[1].Should().Be("Store");
    }

    [Fact]
    public async Task Handle_NewRefreshTokenExpiration_ShouldBe7Days()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var email = "test@example.com";
        var passwordHash = "hashedPassword";
        var oldRefreshToken = "old_refresh_token";
        var newAccessToken = "new_access_token";
        var newRefreshToken = "new_refresh_token";

        var user = new User(email, passwordHash, "John", "Doe", RoleType.Student, tenantId);
        var userId = user.Id;

        _mockRefreshTokenRepository
            .Setup(x => x.ValidateRefreshTokenAsync(oldRefreshToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userId);

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockRefreshTokenRepository
            .Setup(x => x.RevokeRefreshTokenAsync(oldRefreshToken, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockJwtTokenService
            .Setup(x => x.GenerateAccessToken(user))
            .Returns(newAccessToken);

        _mockJwtTokenService
            .Setup(x => x.GenerateRefreshToken())
            .Returns(newRefreshToken);

        DateTime? capturedExpiration = null;
        _mockRefreshTokenRepository
            .Setup(x => x.StoreRefreshTokenAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .Callback<Guid, string, DateTime, CancellationToken>((_, _, exp, _) => capturedExpiration = exp)
            .Returns(Task.CompletedTask);

        var command = new RefreshTokenCommand(oldRefreshToken);
        var beforeExecution = DateTime.UtcNow;

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedExpiration.Should().NotBeNull();
        capturedExpiration!.Value.Should().BeCloseTo(beforeExecution.AddDays(7), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Handle_DifferentUserRoles_ShouldGenerateTokensForAllRoles()
    {
        // Arrange
        var testCases = new[]
        {
            RoleType.Student,
            RoleType.Instructor,
            RoleType.CompanyAdmin
        };

        foreach (var role in testCases)
        {
            var tenantId = Guid.NewGuid();
            var email = $"{role.ToString().ToLower()}@example.com";
            var passwordHash = "hashedPassword";
            var oldRefreshToken = $"old_refresh_token_{role}";
            var newAccessToken = $"new_access_token_{role}";
            var newRefreshToken = $"new_refresh_token_{role}";

            var user = new User(email, passwordHash, "John", "Doe", role, tenantId);
            var userId = user.Id;

            _mockRefreshTokenRepository
                .Setup(x => x.ValidateRefreshTokenAsync(oldRefreshToken, It.IsAny<CancellationToken>()))
                .ReturnsAsync(userId);

            _mockUserRepository
                .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _mockRefreshTokenRepository
                .Setup(x => x.RevokeRefreshTokenAsync(oldRefreshToken, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockJwtTokenService
                .Setup(x => x.GenerateAccessToken(user))
                .Returns(newAccessToken);

            _mockJwtTokenService
                .Setup(x => x.GenerateRefreshToken())
                .Returns(newRefreshToken);

            _mockRefreshTokenRepository
                .Setup(x => x.StoreRefreshTokenAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var command = new RefreshTokenCommand(oldRefreshToken);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.AccessToken.Should().Be(newAccessToken);
            result.RefreshToken.Should().Be(newRefreshToken);
        }
    }
}
