using Maken.Domain.Entities;

namespace Maken.Api.DTOs.Responses;

/// <summary>
/// Response DTO for contact inquiry.
/// </summary>
public class ContactInquiryResponse
{
    /// <summary>
    /// Inquiry unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Name of person contacting.
    /// </summary>
    public string ContactName { get; set; } = string.Empty;

    /// <summary>
    /// Contact email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Name of organization inquiring.
    /// </summary>
    public string OrganizationName { get; set; } = string.Empty;

    /// <summary>
    /// Inquiry message content.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Current inquiry status.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// UTC timestamp when inquiry was submitted.
    /// </summary>
    public DateTime SubmittedAt { get; set; }

    /// <summary>
    /// UTC timestamp when inquiry was reviewed (optional).
    /// </summary>
    public DateTime? ReviewedAt { get; set; }

    /// <summary>
    /// User ID of admin who reviewed (optional).
    /// </summary>
    public Guid? ReviewedBy { get; set; }

    /// <summary>
    /// Admin notes (optional).
    /// </summary>
    public string? Notes { get; set; }
}
