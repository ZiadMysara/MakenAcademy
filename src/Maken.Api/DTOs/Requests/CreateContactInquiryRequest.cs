using System.ComponentModel.DataAnnotations;

namespace Maken.Api.DTOs.Requests;

/// <summary>
/// Request DTO for creating a new contact inquiry from the landing page.
/// </summary>
public class CreateContactInquiryRequest
{
    /// <summary>
    /// Name of person contacting (1-100 characters).
    /// </summary>
    [Required(ErrorMessage = "Contact name is required.")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Contact name must be between 1 and 100 characters.")]
    public string ContactName { get; set; } = string.Empty;

    /// <summary>
    /// Contact email address (valid email format, max 255 characters).
    /// </summary>
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Email format is invalid.")]
    [StringLength(255, ErrorMessage = "Email must not exceed 255 characters.")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Name of organization inquiring (1-200 characters).
    /// </summary>
    [Required(ErrorMessage = "Organization name is required.")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Organization name must be between 1 and 200 characters.")]
    public string OrganizationName { get; set; } = string.Empty;

    /// <summary>
    /// Inquiry message content (10-2000 characters).
    /// </summary>
    [Required(ErrorMessage = "Message is required.")]
    [StringLength(2000, MinimumLength = 10, ErrorMessage = "Message must be between 10 and 2000 characters.")]
    public string Message { get; set; } = string.Empty;
}
