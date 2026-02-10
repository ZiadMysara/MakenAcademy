using FluentValidation;
using FluentValidation.Results;
using Maken.Application.Common.Behaviors;
using MediatR;
using Moq;

namespace Maken.Application.Tests.Common.Behaviors;

/// <summary>
/// Tests for ValidationBehavior to verify APP-007 to APP-012.
/// Ensures validation runs before handlers and rejects invalid requests.
/// </summary>
public class ValidationBehaviorTests
{
    private readonly Mock<RequestHandlerDelegate<string>> _mockNext;

    public ValidationBehaviorTests()
    {
        _mockNext = new Mock<RequestHandlerDelegate<string>>();
        _mockNext.Setup(x => x()).ReturnsAsync("Success");
    }

    [Fact]
    public async Task Handle_NoValidators_ShouldCallNext()
    {
        // Arrange
        var validators = Enumerable.Empty<IValidator<TestRequest>>();
        var behavior = new ValidationBehavior<TestRequest, string>(validators);
        var request = new TestRequest();

        // Act
        var result = await behavior.Handle(request, _mockNext.Object, CancellationToken.None);

        // Assert
        Assert.Equal("Success", result);
        _mockNext.Verify(x => x(), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldCallNext()
    {
        // Arrange
        var mockValidator = new Mock<IValidator<TestRequest>>();
        mockValidator
            .Setup(x => x.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        var validators = new[] { mockValidator.Object };
        var behavior = new ValidationBehavior<TestRequest, string>(validators);
        var request = new TestRequest();

        // Act
        var result = await behavior.Handle(request, _mockNext.Object, CancellationToken.None);

        // Assert
        Assert.Equal("Success", result);
        _mockNext.Verify(x => x(), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidRequest_ShouldThrowValidationException()
    {
        // Arrange
        var mockValidator = new Mock<IValidator<TestRequest>>();
        var validationFailure = new ValidationFailure("Property", "Error message");
        mockValidator
            .Setup(x => x.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[] { validationFailure }));

        var validators = new[] { mockValidator.Object };
        var behavior = new ValidationBehavior<TestRequest, string>(validators);
        var request = new TestRequest();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            behavior.Handle(request, _mockNext.Object, CancellationToken.None));

        Assert.Single(exception.Errors);
        Assert.Equal("Property", exception.Errors.First().PropertyName);
        Assert.Equal("Error message", exception.Errors.First().ErrorMessage);

        // Verify handler was NOT called (APP-011: validation failures don't reach domain)
        _mockNext.Verify(x => x(), Times.Never);
    }

    [Fact]
    public async Task Handle_MultipleValidators_AllFailures_ShouldCollectAllErrors()
    {
        // Arrange
        var mockValidator1 = new Mock<IValidator<TestRequest>>();
        mockValidator1
            .Setup(x => x.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[] { new ValidationFailure("Prop1", "Error 1") }));

        var mockValidator2 = new Mock<IValidator<TestRequest>>();
        mockValidator2
            .Setup(x => x.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[] { new ValidationFailure("Prop2", "Error 2") }));

        var validators = new[] { mockValidator1.Object, mockValidator2.Object };
        var behavior = new ValidationBehavior<TestRequest, string>(validators);
        var request = new TestRequest();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            behavior.Handle(request, _mockNext.Object, CancellationToken.None));

        Assert.Equal(2, exception.Errors.Count());
        _mockNext.Verify(x => x(), Times.Never);
    }

    // Test request class
    public class TestRequest : IRequest<string>
    {
    }
}
