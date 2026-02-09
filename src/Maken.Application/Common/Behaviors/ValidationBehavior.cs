using FluentValidation;
using MediatR;

namespace Maken.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior that validates requests using FluentValidation.
/// Runs before the request handler executes.
/// </summary>
/// <typeparam name="TRequest">The type of request being validated.</typeparam>
/// <typeparam name="TResponse">The type of response returned by the handler.</typeparam>
/// <remarks>
/// This behavior automatically discovers and executes all validators registered for the request type.
/// If validation fails, it throws a ValidationException with all validation errors.
/// 
/// <para><strong>Usage Pattern:</strong></para>
/// <code>
/// // 1. Create a validator for your command/query
/// public class CreateUserCommandValidator : AbstractValidator&lt;CreateUserCommand&gt;
/// {
///     public CreateUserCommandValidator()
///     {
///         RuleFor(x => x.Email).NotEmpty().EmailAddress();
///         RuleFor(x => x.TenantId).NotEmpty().When(x => x.Role != RoleType.PlatformAdmin);
///     }
/// }
/// 
/// // 2. Register the validator in DependencyInjection.cs
/// services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
/// 
/// // 3. Register this behavior in the MediatR pipeline
/// services.AddTransient(typeof(IPipelineBehavior&lt;,&gt;), typeof(ValidationBehavior&lt;,&gt;));
/// 
/// // 4. Validation runs automatically before your handler
/// public class CreateUserCommandHandler : IRequestHandler&lt;CreateUserCommand, Guid&gt;
/// {
///     public async Task&lt;Guid&gt; Handle(CreateUserCommand request, CancellationToken cancellationToken)
///     {
///         // Validation already passed if we reach here
///         var user = new User(request.Email, ...);
///         // ...
///     }
/// }
/// </code>
/// </remarks>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    /// <summary>
    /// Initializes a new instance of the ValidationBehavior class.
    /// </summary>
    /// <param name="validators">Collection of validators for the request type (injected by DI).</param>
    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    /// <summary>
    /// Executes validation before the request handler.
    /// </summary>
    /// <param name="request">The request being validated.</param>
    /// <param name="next">The next behavior or handler in the pipeline.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The response from the handler.</returns>
    /// <exception cref="ValidationException">Thrown when validation fails.</exception>
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // If no validators are registered for this request type, skip validation
        if (!_validators.Any())
        {
            return await next();
        }

        // Create validation context
        var context = new ValidationContext<TRequest>(request);

        // Run all validators in parallel
        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken))
        );

        // Collect all validation failures
        var failures = validationResults
            .Where(r => !r.IsValid)
            .SelectMany(r => r.Errors)
            .ToList();

        // If there are failures, throw ValidationException
        if (failures.Any())
        {
            throw new ValidationException(failures);
        }

        // Validation passed, proceed to the next behavior or handler
        return await next();
    }
}
