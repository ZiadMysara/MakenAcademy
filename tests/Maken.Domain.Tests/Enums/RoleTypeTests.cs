using Maken.Domain.Enums;

namespace Maken.Domain.Tests.Enums;

/// <summary>
/// Tests for RoleType enum to ensure Constitution compliance.
/// Constitution §4 defines exactly 4 roles with specific values.
/// </summary>
public class RoleTypeTests
{
    [Fact]
    public void RoleType_ShouldHaveExactlyFourRoles()
    {
        // Arrange & Act
        var roleCount = Enum.GetValues<RoleType>().Length;

        // Assert
        Assert.Equal(4, roleCount);
    }

    [Fact]
    public void RoleType_PlatformAdmin_ShouldHaveValueZero()
    {
        // Arrange & Act
        var value = (int)RoleType.PlatformAdmin;

        // Assert
        Assert.Equal(0, value);
    }

    [Fact]
    public void RoleType_CompanyAdmin_ShouldHaveValueOne()
    {
        // Arrange & Act
        var value = (int)RoleType.CompanyAdmin;

        // Assert
        Assert.Equal(1, value);
    }

    [Fact]
    public void RoleType_Instructor_ShouldHaveValueTwo()
    {
        // Arrange & Act
        var value = (int)RoleType.Instructor;

        // Assert
        Assert.Equal(2, value);
    }

    [Fact]
    public void RoleType_Student_ShouldHaveValueThree()
    {
        // Arrange & Act
        var value = (int)RoleType.Student;

        // Assert
        Assert.Equal(3, value);
    }

    [Fact]
    public void RoleType_ShouldContainAllRequiredRoles()
    {
        // Arrange
        var expectedRoles = new[]
        {
            RoleType.PlatformAdmin,
            RoleType.CompanyAdmin,
            RoleType.Instructor,
            RoleType.Student
        };

        // Act
        var actualRoles = Enum.GetValues<RoleType>();

        // Assert
        Assert.Equal(expectedRoles.Length, actualRoles.Length);
        foreach (var expectedRole in expectedRoles)
        {
            Assert.Contains(expectedRole, actualRoles);
        }
    }

    [Theory]
    [InlineData("PlatformAdmin")]
    [InlineData("CompanyAdmin")]
    [InlineData("Instructor")]
    [InlineData("Student")]
    public void RoleType_ShouldParseFromString(string roleName)
    {
        // Act
        var canParse = Enum.TryParse<RoleType>(roleName, out var role);

        // Assert
        Assert.True(canParse);
        Assert.Equal(roleName, role.ToString());
    }
}
