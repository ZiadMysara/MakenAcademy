namespace Maken.Api.DTOs.Responses;

/// <summary>
/// Response DTO for paginated course list.
/// </summary>
public record CourseListResponse(
    List<CourseResponse> Courses,
    int TotalCount,
    int PageNumber,
    int PageSize
);
