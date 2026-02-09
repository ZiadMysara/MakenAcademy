namespace Maken.Api.Models;

/// <summary>
/// Generic wrapper for API responses.
/// </summary>
/// <typeparam name="T">Type of the response data</typeparam>
public class ApiResponse<T>
{
    /// <summary>
    /// Whether the request was successful.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Response payload (null on error).
    /// </summary>
    public T? Data { get; init; }

    /// <summary>
    /// Error information (null on success).
    /// </summary>
    public ApiError? Error { get; init; }

    /// <summary>
    /// Creates a successful response.
    /// </summary>
    public static ApiResponse<T> Ok(T data) => new()
    {
        Success = true,
        Data = data,
        Error = null
    };

    /// <summary>
    /// Creates an error response.
    /// </summary>
    public static ApiResponse<T> Fail(string code, string message, Dictionary<string, object>? details = null) => new()
    {
        Success = false,
        Data = default,
        Error = new ApiError
        {
            Code = code,
            Message = message,
            Details = details
        }
    };
}
