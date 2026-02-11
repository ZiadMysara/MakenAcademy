using Maken.Domain.Common;

namespace Maken.Domain.Entities;

/// <summary>
/// Represents a contact inquiry submitted through the public landing page.
/// This entity is NOT tenant-scoped as it represents inquiries from potential new organizations.
/// </summary>
public class ContactInquiry : BaseEntity
{
    /// <summary>
    /// Name of the person contacting.
    /// Required, 1-100 characters.
    /// </summary>
    public string ContactName { get; private set; } = string.Empty;

    /// <summary>
    /// Contact email address.
    /// Required, valid email format, max 255 characters.
    /// </summary>
    public string Email { get; private set; } = string.Empty;

    /// <summary>
    /// Name of the organization inquiring.
    /// Required, 1-200 characters.
    /// </summary>
    public string OrganizationName { get; private set; } = string.Empty;

    /// <summary>
    /// Inquiry message content.
    /// Required, 10-2000 characters.
    /// </summary>
    public string Message { get; private set; } = string.Empty;

    /// <summary>
    /// Current status of the inquiry.
    /// Defaults to New when created.
    /// </summary>
    public ContactInquiryStatus Status { get; private set; }

    /// <summary>
    /// UTC timestamp when the inquiry was submitted.
    /// </summary>
    public DateTime SubmittedAt { get; private set; }

    /// <summary>
    /// UTC timestamp when the inquiry was first reviewed by an admin (optional).
    /// Set automatically when status changes from New.
    /// </summary>
    public DateTime? ReviewedAt { get; private set; }

    /// <summary>
    /// User ID of the admin who reviewed the inquiry (optional).
    /// </summary>
    public Guid? ReviewedBy { get; private set; }

    /// <summary>
    /// Admin notes about the inquiry (optional).
    /// Max 1000 characters.
    /// </summary>
    public string? Notes { get; private set; }

    /// <summary>
    /// Private constructor for EF Core.
    /// </summary>
    private ContactInquiry() : base()
    {
    }

    /// <summary>
    /// Creates a new contact inquiry.
    /// </summary>
    /// <param name="contactName">Name of person contacting (1-100 chars)</param>
    /// <param name="email">Contact email address (valid email, max 255 chars)</param>
    /// <param name="organizationName">Name of organization (1-200 chars)</param>
    /// <param name="message">Inquiry message (10-2000 chars)</param>
    /// <returns>A new ContactInquiry instance</returns>
    /// <exception cref="ArgumentException">Thrown when validation fails</exception>
    public static ContactInquiry Create(
        string contactName,
        string email,
        string organizationName,
        string message)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(contactName))
            throw new ArgumentException("Contact name is required.", nameof(contactName));
        
        if (contactName.Length > 100)
            throw new ArgumentException("Contact name must not exceed 100 characters.", nameof(contactName));

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.", nameof(email));
        
        if (email.Length > 255)
            throw new ArgumentException("Email must not exceed 255 characters.", nameof(email));

        if (!IsValidEmail(email))
            throw new ArgumentException("Email format is invalid.", nameof(email));

        if (string.IsNullOrWhiteSpace(organizationName))
            throw new ArgumentException("Organization name is required.", nameof(organizationName));
        
        if (organizationName.Length > 200)
            throw new ArgumentException("Organization name must not exceed 200 characters.", nameof(organizationName));

        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message is required.", nameof(message));
        
        if (message.Length < 10)
            throw new ArgumentException("Message must be at least 10 characters.", nameof(message));
        
        if (message.Length > 2000)
            throw new ArgumentException("Message must not exceed 2000 characters.", nameof(message));

        var inquiry = new ContactInquiry
        {
            ContactName = contactName.Trim(),
            Email = email.Trim().ToLowerInvariant(),
            OrganizationName = organizationName.Trim(),
            Message = message.Trim(),
            Status = ContactInquiryStatus.New,
            SubmittedAt = DateTime.UtcNow
        };

        return inquiry;
    }

    /// <summary>
    /// Updates the status of the inquiry.
    /// Automatically sets ReviewedAt and ReviewedBy on first transition from New.
    /// </summary>
    /// <param name="newStatus">The new status</param>
    /// <param name="reviewedBy">User ID of the admin reviewing (optional)</param>
    /// <param name="notes">Admin notes (optional, max 1000 chars)</param>
    /// <exception cref="ArgumentException">Thrown when validation fails</exception>
    public void UpdateStatus(ContactInquiryStatus newStatus, Guid? reviewedBy = null, string? notes = null)
    {
        // Validate notes length
        if (notes != null && notes.Length > 1000)
            throw new ArgumentException("Notes must not exceed 1000 characters.", nameof(notes));

        // Set ReviewedAt and ReviewedBy on first transition from New
        if (Status == ContactInquiryStatus.New && newStatus != ContactInquiryStatus.New)
        {
            ReviewedAt = DateTime.UtcNow;
            ReviewedBy = reviewedBy;
        }

        Status = newStatus;
        Notes = notes;
        SetUpdatedBy(reviewedBy ?? Guid.Empty);
    }

    /// <summary>
    /// Simple email validation using basic regex pattern.
    /// </summary>
    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Status of a contact inquiry.
/// </summary>
public enum ContactInquiryStatus
{
    /// <summary>
    /// Initial status when submitted. Inquiry has not been reviewed yet.
    /// </summary>
    New = 0,

    /// <summary>
    /// Admin has reviewed the inquiry.
    /// </summary>
    Reviewed = 1,

    /// <summary>
    /// Admin has contacted the organization.
    /// </summary>
    Contacted = 2
}
