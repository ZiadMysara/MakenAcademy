using System.ComponentModel.DataAnnotations;
using Maken.Domain.Entities;

namespace Maken.Api.DTOs.Requests;

/// <summary>
/// Request DTO for updating contact inquiry status (admin only).
/// </summary>
public class UpdateContactInquiryStatusRequest
{
    /// <summary>
    /// New status for the inquiry.
    /// </summary>
    [Required(ErrorMessage = "Status is required.")]
    public ContactInquiryStatus Status { get; set; }

    /// <summary>
    /// Admin notes (optional, max 1000 characters).
    /// </summary>
    [StringLength(1000, ErrorMessage = "Notes must not exceed 1000 characters.")]
    public string? Notes { get; set; }
}
