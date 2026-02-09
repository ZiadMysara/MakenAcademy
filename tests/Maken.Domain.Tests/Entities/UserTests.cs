using Maken.Domain.Entities;
using Maken.Domain.Enums;

namespace Maken.Domain.Tests.Entities;

/// <summary>
/// Tests for User entity to ensure Constitution compliance.
/// Constitution requirement: TenantId is null ONLY for PlatformAdmin role.
/// </summary>
public class UserTests
{
    private readonly Guid _validTenantId = Guid.NewGuid();

    #region TenantId Validation Tests

    [Fact]
    public void User_PlatformAdmin_ShouldAllowNullTenantId()
    {
        // Arrange & Act
        var user = new User(
            email: "admin@maken.app",
            passwordHash: "hashedpassword",
            firstName: "Platform",
            lastName: "Admin",
            role: RoleType.PlatformAdmin,
            tenantId: null
        );

        // Assert
        Assert.Equal(RoleType.PlatformAdmin, user.Role);
        Assert.Null(user.TenantId);
    }

    [Fact]
    public void User_PlatformAdmin_ShouldRejectNonNullTenantId()
    {
        // Arrange, Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new User(
            email: "admin@maken.app",
            passwordHash: "hashedpassword",
            firstName: "Platform",
            lastName: "Admin",
            role: RoleType.PlatformAdmin,
            tenantId: _validTenantId
        ));

        Assert.Contains("PlatformAdmin cannot belong to a tenant", exception.Message);
    }

    [Fact]
    public void User_CompanyAdmin_ShouldRequireTenantId()
    {
        // Arrange & Act
        var user = new User(
            email: "admin@academy.com",
            passwordHash: "hashedpassword",
            firstName: "Company",
            lastName: "Admin",
            role: RoleType.CompanyAdmin,
            tenantId: _validTenantId
        );

        // Assert
        Assert.Equal(RoleType.CompanyAdmin, user.Role);
        Assert.Equal(_validTenantId, user.TenantId);
    }

    [Fact]
    public void User_CompanyAdmin_ShouldRejectNullTenantId()
    {
        // Arrange, Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new User(
            email: "admin@academy.com",
            passwordHash: "hashedpassword",
            firstName: "Company",
            lastName: "Admin",
            role: RoleType.CompanyAdmin,
            tenantId: null
        ));

        Assert.Contains("CompanyAdmin must belong to a tenant", exception.Message);
    }

    [Fact]
    public void User_Instructor_ShouldRequireTenantId()
    {
        // Arrange & Act
        var user = new User(
            email: "instructor@academy.com",
            passwordHash: "hashedpassword",
            firstName: "John",
            lastName: "Instructor",
            role: RoleType.Instructor,
            tenantId: _validTenantId
        );

        // Assert
        Assert.Equal(RoleType.Instructor, user.Role);
        Assert.Equal(_validTenantId, user.TenantId);
    }

    [Fact]
    public void User_Instructor_ShouldRejectNullTenantId()
    {
        // Arrange, Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new User(
            email: "instructor@academy.com",
            passwordHash: "hashedpassword",
            firstName: "John",
            lastName: "Instructor",
            role: RoleType.Instructor,
            tenantId: null
        ));

        Assert.Contains("Instructor must belong to a tenant", exception.Message);
    }

    [Fact]
    public void User_Student_ShouldRequireTenantId()
    {
        // Arrange & Act
        var user = new User(
            email: "student@academy.com",
            passwordHash: "hashedpassword",
            firstName: "Jane",
            lastName: "Student",
            role: RoleType.Student,
            tenantId: _validTenantId
        );

        // Assert
        Assert.Equal(RoleType.Student, user.Role);
        Assert.Equal(_validTenantId, user.TenantId);
    }

    [Fact]
    public void User_Student_ShouldRejectNullTenantId()
    {
        // Arrange, Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new User(
            email: "student@academy.com",
            passwordHash: "hashedpassword",
            firstName: "Jane",
            lastName: "Student",
            role: RoleType.Student,
            tenantId: null
        ));

        Assert.Contains("Student must belong to a tenant", exception.Message);
    }

    #endregion

    #region SetRole Method Tests

    [Fact]
    public void SetRole_PlatformAdmin_WithNullTenantId_ShouldSucceed()
    {
        // Arrange
        var user = new User(
            email: "user@academy.com",
            passwordHash: "hashedpassword",
            firstName: "Test",
            lastName: "User",
            role: RoleType.Student,
            tenantId: _validTenantId
        );

        // Act
        user.SetRole(RoleType.PlatformAdmin, null);

        // Assert
        Assert.Equal(RoleType.PlatformAdmin, user.Role);
        Assert.Null(user.TenantId);
    }

    [Fact]
    public void SetRole_PlatformAdmin_WithTenantId_ShouldThrowException()
    {
        // Arrange
        var user = new User(
            email: "user@academy.com",
            passwordHash: "hashedpassword",
            firstName: "Test",
            lastName: "User",
            role: RoleType.Student,
            tenantId: _validTenantId
        );

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            user.SetRole(RoleType.PlatformAdmin, _validTenantId)
        );

        Assert.Contains("PlatformAdmin cannot belong to a tenant", exception.Message);
    }

    [Fact]
    public void SetRole_NonPlatformAdmin_WithNullTenantId_ShouldThrowException()
    {
        // Arrange
        var user = new User(
            email: "admin@maken.app",
            passwordHash: "hashedpassword",
            firstName: "Platform",
            lastName: "Admin",
            role: RoleType.PlatformAdmin,
            tenantId: null
        );

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            user.SetRole(RoleType.CompanyAdmin, null)
        );

        Assert.Contains("CompanyAdmin must belong to a tenant", exception.Message);
    }

    [Fact]
    public void SetRole_NonPlatformAdmin_WithTenantId_ShouldSucceed()
    {
        // Arrange
        var user = new User(
            email: "admin@maken.app",
            passwordHash: "hashedpassword",
            firstName: "Platform",
            lastName: "Admin",
            role: RoleType.PlatformAdmin,
            tenantId: null
        );

        // Act
        user.SetRole(RoleType.CompanyAdmin, _validTenantId);

        // Assert
        Assert.Equal(RoleType.CompanyAdmin, user.Role);
        Assert.Equal(_validTenantId, user.TenantId);
    }

    #endregion

    #region Basic User Creation Tests

    [Fact]
    public void User_Constructor_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var email = "test@academy.com";
        var passwordHash = "hashedpassword123";
        var firstName = "John";
        var lastName = "Doe";
        var role = RoleType.Student;

        // Act
        var user = new User(email, passwordHash, firstName, lastName, role, _validTenantId);

        // Assert
        Assert.Equal(email.ToLowerInvariant(), user.Email);
        Assert.Equal(passwordHash, user.PasswordHash);
        Assert.Equal(firstName, user.FirstName);
        Assert.Equal(lastName, user.LastName);
        Assert.Equal(role, user.Role);
        Assert.Equal(_validTenantId, user.TenantId);
        Assert.True(user.IsActive);
        Assert.Null(user.LastLoginAt);
    }

    [Fact]
    public void User_FullName_ShouldCombineFirstAndLastName()
    {
        // Arrange & Act
        var user = new User(
            email: "test@academy.com",
            passwordHash: "hashedpassword",
            firstName: "John",
            lastName: "Doe",
            role: RoleType.Student,
            tenantId: _validTenantId
        );

        // Assert
        Assert.Equal("John Doe", user.FullName);
    }

    [Fact]
    public void User_IsActive_ShouldBeTrue_ByDefault()
    {
        // Arrange & Act
        var user = new User(
            email: "test@academy.com",
            passwordHash: "hashedpassword",
            firstName: "John",
            lastName: "Doe",
            role: RoleType.Student,
            tenantId: _validTenantId
        );

        // Assert
        Assert.True(user.IsActive);
    }

    #endregion
}
