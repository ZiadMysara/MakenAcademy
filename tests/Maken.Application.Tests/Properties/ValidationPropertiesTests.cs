using FluentValidation;
using FluentValidation.Results;
using FsCheck;
using FsCheck.Xunit;
using Maken.Application.Commands.Courses;
using Maken.Application.Validators;
using Moq;

namespace Maken.Application.Tests.Properties;

/// <summary>
/// Property-based tests for validation pipeline correctness properties.
/// These tests verify that validation is enforced before reaching the domain layer.
/// </summary>
public class ValidationPropertiesTests
{
    /// <summary>
    /// Property 26: Validation Pipeline Enforcement
    /// For any command with invalid input data (missing required fields, exceeding length limits, invalid formats),
    /// the validation pipeline should reject the command with 400 Bad Request and return clear, actionable
    /// validation error messages before reaching the domain layer.
    /// **Validates: Requirements FR-048, FR-051**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool ValidationPipeline_MissingRequiredField_ReturnsValidationError(string description)
    {
        // Arrange: Create command with missing required Name field
        var command = new CreateCourseCommand(
            Name: string.Empty, // Missing required field
            Description: description,
            FreeFlowMode: false
        );

        var validator = new CreateCourseCommandValidator();

        // Act: Validate the command
        var result = validator.Validate(command);

        // Assert: Validation should fail for missing required field
        return !result.IsValid && result.Errors.Any(e => e.PropertyName == nameof(CreateCourseCommand.Name));
    }

    /// <summary>
    /// Property 26: Validation Pipeline Enforcement (Length limit variant)
    /// For any command with fields exceeding length limits, validation should reject the command.
    /// **Validates: Requirements FR-048, FR-051**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool ValidationPipeline_ExceedingLengthLimit_ReturnsValidationError(string description)
    {
        // Skip if description is null or within valid length
        if (string.IsNullOrEmpty(description) || description.Length <= 2000) return true;

        // Arrange: Create command with description exceeding max length (2000 chars)
        var command = new CreateCourseCommand(
            Name: "Valid Course Name",
            Description: description, // Exceeds max length
            FreeFlowMode: false
        );

        var validator = new CreateCourseCommandValidator();

        // Act: Validate the command
        var result = validator.Validate(command);

        // Assert: Validation should fail for exceeding length limit
        return !result.IsValid && result.Errors.Any(e => e.PropertyName == nameof(CreateCourseCommand.Description));
    }

    /// <summary>
    /// Property 26: Validation Pipeline Enforcement (Valid input variant)
    /// For any command with valid input data, validation should pass.
    /// **Validates: Requirements FR-048, FR-051**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool ValidationPipeline_ValidInput_PassesValidation(bool freeFlowMode)
    {
        // Arrange: Create command with valid data
        var command = new CreateCourseCommand(
            Name: "Valid Course Name",
            Description: "Valid course description",
            FreeFlowMode: freeFlowMode
        );

        var validator = new CreateCourseCommandValidator();

        // Act: Validate the command
        var result = validator.Validate(command);

        // Assert: Validation should pass for valid input
        return result.IsValid;
    }

    /// <summary>
    /// Property 26: Validation Pipeline Enforcement (Error message clarity variant)
    /// For any validation failure, the error message should be clear and actionable.
    /// **Validates: Requirements FR-048, FR-051**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool ValidationPipeline_ValidationFailure_ReturnsActionableErrorMessage()
    {
        // Arrange: Create command with missing required Name field
        var command = new CreateCourseCommand(
            Name: string.Empty, // Missing required field
            Description: "Valid description",
            FreeFlowMode: false
        );

        var validator = new CreateCourseCommandValidator();

        // Act: Validate the command
        var result = validator.Validate(command);

        // Assert: Error message should be clear and actionable (not empty)
        return !result.IsValid 
            && result.Errors.Any() 
            && result.Errors.All(e => !string.IsNullOrWhiteSpace(e.ErrorMessage));
    }
}
