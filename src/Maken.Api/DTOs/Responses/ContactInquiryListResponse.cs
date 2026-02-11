namespace Maken.Api.DTOs.Responses;

/// <summary>
/// Response DTO for paginated list of contact inquiries.
/// </summary>
public class ContactInquiryListResponse
{
    /// <summary>
    /// List of contact inquiries for current page.
    /// </summary>
    public List<ContactInquiryResponse> Items { get; set; } = new();

    /// <summary>
    /// Total number of inquiries matching filter.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Current page number (1-based).
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Number of items per page.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of pages.
    /// </summary>
    public int TotalPages { get; set; }
}
