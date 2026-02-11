using FluentAssertions;
using Maken.Application.Commands.Auth;
using Maken.Application.Common.Interfaces;
using Maken.Domain.Entities;
using Maken.Domain.Enums;
using Moq;
using Microsoft.Extensions.Logging;

namespace Maken.Application.Tests.Commands;

/// <summary>
/// Unit tests for LoginCommandHandler.
/// Validates authentication logic, password verification, and token generation.
/// </summary>
public class LoginCommandTests
{
    private readonly Mock<IRepository<User>> _mockUserRepository;
    private readonly Mock<IJwtTokenService> _mockJwtTokenService;
    private readonly Mock<IPasswordHasher> _mockPasswordHasher;
    private readonly Mock<IRefreshTokenRepository> _mockRefreshTokenRepository;
    private readonly LoginCommandHandler _handler;

    public LoginCommandTests()
    {
        _mockUserRepository = new Mock<IRepository<User>>();
        _mockJwtTokenService = new Mock<IJwtTokenService>();
        _mockPasswordHasher = new Mock<IPasswordHasher>();
        _mockRefreshTokenRepository = new Mock<IRefreshTokenRepository>();

        _handler = new LoginCommandHandler(
            _mockUserRepository.Object,
            _mockJwtTokenService.Object,
            _mockPasswordHasher.Object,
            _mockRefreshTokenRepository.Object,
            new Mock<ILogger<LoginCommandHandler>>().Object);
    }

    [Fact]
    public async Task Handle_ValidCredentials_ShouldReturnLoginResult()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var email = "test@example.com";
        var password = "Password123!";
        var passwordHash = "hashedPassword";
        var accessToken = "access_token_jwt";
        var refreshToken = "refresh_token_string";

        var user = new User(email, passwordHash, "John", "Doe", RoleType.Student, tenantId);
        var userId = user.Id; // Use the auto-generated ID

        _mockUserRepository
            .Setup(x => x.GetFirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockPasswordHasher
            .Setup(x => x.VerifyPassword(password, passwordHash))
            .Returns(true);

        _mockJwtTokenService
            .Setup(x => x.GenerateAccessToken(user))
            .Returns(accessToken);

        _mockJwtTokenService
            .Setup(x => x.GenerateRefreshToken())
            .Returns(refreshToken);

        _mockRefreshTokenRepository
            .Setup(x => x.StoreRefreshTokenAsync(
                userId,
                refreshToken,
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var command = new LoginCommand(email, password);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be(accessToken);
        result.RefreshToken.Should().Be(refreshToken);
        result.UserId.Should().Be(userId);
        result.Email.Should().Be(email);
        result.Role.Should().Be(RoleType.Student.ToString());

        _mockRefreshTokenRepository.Verify(
            x => x.StoreRefreshTokenAsync(
                userId,
                refreshToken,
                It.Is<DateTime>(dt => dt > DateTime.UtcNow.AddDays(6) && dt <= DateTime.UtcNow.AddDays(7)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_UserNotFound_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var email = "nonexistent@example.com";
        var password = "Password123!";

        _mockUserRepository
            .Setup(x => x.GetFirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var command = new LoginCommand(email, password);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Be("Invalid email or password.");

        _mockPasswordHasher.Verify(
            x => x.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_InvalidPassword_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var email = "test@example.com";
        var password = "WrongPassword";
        var passwordHash = "hashedPassword";

        var user = new User(email, passwordHash, "John", "Doe", RoleType.Student, tenantId);
        var userId = user.Id; // Use the auto-generated ID

        _mockUserRepository
            .Setup(x => x.GetFirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockPasswordHasher
            .Setup(x => x.VerifyPassword(password, passwordHash))
            .Returns(false);

        var command = new LoginCommand(email, password);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Be("Invalid email or password.");

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
        var password = "Password123!";
        var passwordHash = "hashedPassword";

        var user = new User(email, passwordHash, "John", "Doe", RoleType.Student, tenantId);
        var userId = user.Id; // Use the auto-generated ID
        user.SoftDelete(); // Soft delete the user

        _mockUserRepository
            .Setup(x => x.GetFirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockPasswordHasher
            .Setup(x => x.VerifyPassword(password, passwordHash))
            .Returns(true);

        var command = new LoginCommand(email, password);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Be("User account is inactive.");

        _mockJwtTokenService.Verify(
            x => x.GenerateAccessToken(It.IsAny<User>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_EmailCaseInsensitive_ShouldFindUser()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var email = "test@example.com";
        var emailUpperCase = "TEST@EXAMPLE.COM";
        var password = "Password123!";
        var passwordHash = "hashedPassword";
        var accessToken = "access_token_jwt";
        var refreshToken = "refresh_token_string";

        var user = new User(email, passwordHash, "John", "Doe", RoleType.Student, tenantId);
        var userId = user.Id; // Use the auto-generated ID

        _mockUserRepository
            .Setup(x => x.GetFirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockPasswordHasher
            .Setup(x => x.VerifyPassword(password, passwordHash))
            .Returns(true);

        _mockJwtTokenService
            .Setup(x => x.GenerateAccessToken(user))
            .Returns(accessToken);

        _mockJwtTokenService
            .Setup(x => x.GenerateRefreshToken())
            .Returns(refreshToken);

        _mockRefreshTokenRepository
            .Setup(x => x.StoreRefreshTokenAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var command = new LoginCommand(emailUpperCase, password);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Email.Should().Be(email);
    }

    [Fact]
    public async Task Handle_DifferentRoles_ShouldReturnCorrectRole()
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
            var password = "Password123!";
            var passwordHash = "hashedPassword";
            var accessToken = "access_token_jwt";
            var refreshToken = "refresh_token_string";

            var user = new User(email, passwordHash, "John", "Doe", role, tenantId);
            var userId = user.Id; // Use the auto-generated ID

            _mockUserRepository
                .Setup(x => x.GetFirstOrDefaultAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _mockPasswordHasher
                .Setup(x => x.VerifyPassword(password, passwordHash))
                .Returns(true);

            _mockJwtTokenService
                .Setup(x => x.GenerateAccessToken(user))
                .Returns(accessToken);

            _mockJwtTokenService
                .Setup(x => x.GenerateRefreshToken())
                .Returns(refreshToken);

            _mockRefreshTokenRepository
                .Setup(x => x.StoreRefreshTokenAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var command = new LoginCommand(email, password);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Role.Should().Be(role.ToString());
        }
    }

    [Fact]
    public async Task Handle_RefreshTokenExpiration_ShouldBe7Days()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var email = "test@example.com";
        var password = "Password123!";
        var passwordHash = "hashedPassword";
        var accessToken = "access_token_jwt";
        var refreshToken = "refresh_token_string";

        var user = new User(email, passwordHash, "John", "Doe", RoleType.Student, tenantId);
        var userId = user.Id; // Use the auto-generated ID

        _mockUserRepository
            .Setup(x => x.GetFirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockPasswordHasher
            .Setup(x => x.VerifyPassword(password, passwordHash))
            .Returns(true);

        _mockJwtTokenService
            .Setup(x => x.GenerateAccessToken(user))
            .Returns(accessToken);

        _mockJwtTokenService
            .Setup(x => x.GenerateRefreshToken())
            .Returns(refreshToken);

        DateTime? capturedExpiration = null;
        _mockRefreshTokenRepository
            .Setup(x => x.StoreRefreshTokenAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .Callback<Guid, string, DateTime, CancellationToken>((_, _, exp, _) => capturedExpiration = exp)
            .Returns(Task.CompletedTask);

        var command = new LoginCommand(email, password);
        var beforeExecution = DateTime.UtcNow;

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedExpiration.Should().NotBeNull();
        capturedExpiration!.Value.Should().BeCloseTo(beforeExecution.AddDays(7), TimeSpan.FromSeconds(5));
    }
}
