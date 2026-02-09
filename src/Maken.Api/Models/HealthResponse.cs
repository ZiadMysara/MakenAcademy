namespace Maken.Api.Models;

/// <summary>
/// Health check response.
/// </summary>
public class HealthResponse
{
    /// <summary>
    /// Overall health status.
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// API version.
    /// </summary>
    public required string Version { get; init; }

    /// <summary>
    /// Timestamp of the health check (UTC).
    /// </summary>
    public required DateTime Timestamp { get; init; }

    /// <summary>
    /// Individual health checks.
    /// </summary>
    public List<HealthCheck> Checks { get; init; } = new();
}

/// <summary>
/// Individual health check result.
/// </summary>
public class HealthCheck
{
    /// <summary>
    /// Name of the check.
    /// </summary>
    /// <example>database</example>
    public required string Name { get; init; }

    /// <summary>
    /// Status of this check.
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Time taken for the check.
    /// </summary>
    /// <example>12ms</example>
    public string? Duration { get; init; }
}
