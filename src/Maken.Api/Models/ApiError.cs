namespace Maken.Api.Models;

/// <summary>
/// Represents an error in an API response.
/// </summary>
public class ApiError
{
    /// <summary>
    /// Machine-readable error code.
    /// </summary>
    /// <example>TENANT_NOT_FOUND</example>
    public required string Code { get; init; }

    /// <summary>
    /// Human-readable error message.
    /// </summary>
    /// <example>The requested tenant does not exist</example>
    public required string Message { get; init; }

    /// <summary>
    /// Additional error context (optional).
    /// </summary>
    public Dictionary<string, object>? Details { get; init; }
}
