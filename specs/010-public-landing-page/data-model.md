# Data Model: Public Landing Page

**Feature**: Public Landing Page  
**Branch**: `010-public-landing-page`  
**Date**: 2025-02-11  
**Phase**: 1 - Design & Contracts

## Purpose

This document defines the data entities, relationships, and validation rules for the public landing page feature.

## Entity Definitions

### 1. ContactInquiry (New Entity)

Represents a contact inquiry submitted through the landing page contact form.

**Table Name**: `ContactInquiries`

**Fields**:

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | Guid (UUID) | Primary Key, Required | Unique identifier |
| ContactName | string | Required, MaxLength(100) | Name of person contacting |
| Email | string | Required, MaxLength(255), Email format | Contact email address |
| OrganizationName | string | Required, MaxLength(200) | Name of organization inquiring |
| Message | string | Required, MaxLength(2000) | Inquiry message content |
| Status | enum | Required, Default: New | Inquiry status (New/Reviewed/Contacted) |
| SubmittedAt | DateTime | Required, Default: UtcNow | Timestamp of submission |
| ReviewedAt | DateTime? | Optional | Timestamp when reviewed by admin |
| ReviewedBy | Guid? | Optional, FK to Users | Admin who reviewed |
| Notes | string? | Optional, MaxLength(1000) | Admin notes |
| IsDeleted | bool | Required, Default: false | Soft delete flag |
| DeletedAt | DateTime? | Optional | Soft delete timestamp |

**Indexes**:
- Primary Key: `Id`
- Index on `Status` (for filtering)
- Index on `SubmittedAt` (for sorting)
- Index on `IsDeleted` (for soft delete queries)

**Validation Rules**:
- ContactName: Required, 1-100 characters, no special characters except spaces, hyphens, apostrophes
- Email: Required, valid email format, max 255 characters
- OrganizationName: Required, 1-200 characters
- Message: Required, 10-2000 characters
- Status: Must be one of: New, Reviewed, Contacted

**Business Rules**:
- New inquiries default to Status = New
- ReviewedAt and ReviewedBy are set when Status changes from New
- Soft delete: IsDeleted = true, DeletedAt = current timestamp
- Deleted inquiries are excluded from default queries

---

### 2. Organization/Tenant (Existing Entity - Read Only)

The landing page reads from the existing Tenant/Organization entity to display available organizations.

**Fields Used** (Read Only):

| Field | Type | Description |
|-------|------|-------------|
| Id | Guid | Organization identifier |
| Name | string | Organization display name |
| Subdomain | string | Subdomain for tenant access |
| LogoUrl | string? | URL to organization logo |
| PrimaryColor | string? | Hex color code for branding |
| SecondaryColor | string? | Hex color code for branding |
| IsActive | bool | Whether organization is active |

**Query Filters**:
- Only organizations where `IsActive = true` are displayed
- Only organizations where `IsDeleted = false` (soft delete)
- Ordered by `Name` ascending

**No Modifications**: Landing page only reads organization data, never modifies it.

---

### 3. LandingPageContent (Future Entity - Not Implemented in MVP)

Placeholder for future CMS capability. Not implemented in MVP - content is hardcoded.

**Planned Fields** (for reference):

| Field | Type | Description |
|-------|------|-------------|
| Id | Guid | Content identifier |
| SectionType | enum | Hero/Features/Benefits/HowItWorks |
| Title | string | Section title |
| Content | string | Section content (HTML/Markdown) |
| DisplayOrder | int | Sort order |
| IsActive | bool | Whether section is visible |

**Note**: This entity is defined for future implementation but not part of MVP scope.

---

## Entity Relationships

```
ContactInquiry
├── ReviewedBy (FK) → User (Optional, for admin tracking)
└── No other relationships (standalone entity)

Organization/Tenant (Read Only)
└── No relationships from landing page perspective
```

**Relationship Rules**:
- ContactInquiry is independent - no foreign keys to other entities except optional ReviewedBy
- Organization data is read-only from landing page
- No cascading deletes (ContactInquiry is standalone)

---

## Enumerations

### ContactInquiryStatus

```csharp
public enum ContactInquiryStatus
{
    New = 0,        // Initial status when submitted
    Reviewed = 1,   // Admin has reviewed the inquiry
    Contacted = 2   // Admin has contacted the organization
}
```

**State Transitions**:
- New → Reviewed (when admin opens/reviews inquiry)
- Reviewed → Contacted (when admin contacts organization)
- Contacted → Reviewed (if follow-up needed)

**Business Rules**:
- Cannot transition back to New once Reviewed
- ReviewedAt timestamp set on first transition from New
- Status changes are audited via ReviewedAt and ReviewedBy

---

## Database Migration

### Migration: AddContactInquiryEntity

**Up**:
```sql
CREATE TABLE "ContactInquiries" (
    "Id" uuid NOT NULL PRIMARY KEY,
    "ContactName" varchar(100) NOT NULL,
    "Email" varchar(255) NOT NULL,
    "OrganizationName" varchar(200) NOT NULL,
    "Message" varchar(2000) NOT NULL,
    "Status" integer NOT NULL DEFAULT 0,
    "SubmittedAt" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
    "ReviewedAt" timestamp with time zone NULL,
    "ReviewedBy" uuid NULL,
    "Notes" varchar(1000) NULL,
    "IsDeleted" boolean NOT NULL DEFAULT false,
    "DeletedAt" timestamp with time zone NULL,
    CONSTRAINT "FK_ContactInquiries_Users_ReviewedBy" FOREIGN KEY ("ReviewedBy") REFERENCES "Users" ("Id") ON DELETE SET NULL
);

CREATE INDEX "IX_ContactInquiries_Status" ON "ContactInquiries" ("Status");
CREATE INDEX "IX_ContactInquiries_SubmittedAt" ON "ContactInquiries" ("SubmittedAt" DESC);
CREATE INDEX "IX_ContactInquiries_IsDeleted" ON "ContactInquiries" ("IsDeleted");
```

**Down**:
```sql
DROP TABLE "ContactInquiries";
```

---

## Data Access Patterns

### Query Patterns

**1. Get Active Organizations (Public)**
```sql
SELECT Id, Name, Subdomain, LogoUrl, PrimaryColor, SecondaryColor
FROM Tenants
WHERE IsActive = true AND IsDeleted = false
ORDER BY Name ASC;
```

**2. Create Contact Inquiry**
```sql
INSERT INTO ContactInquiries (Id, ContactName, Email, OrganizationName, Message, Status, SubmittedAt, IsDeleted)
VALUES (@Id, @ContactName, @Email, @OrganizationName, @Message, 0, @SubmittedAt, false);
```

**3. Get Contact Inquiries (Admin)**
```sql
SELECT *
FROM ContactInquiries
WHERE IsDeleted = false
  AND (@Status IS NULL OR Status = @Status)
ORDER BY SubmittedAt DESC
LIMIT @PageSize OFFSET @Offset;
```

**4. Update Inquiry Status (Admin)**
```sql
UPDATE ContactInquiries
SET Status = @Status,
    ReviewedAt = CASE WHEN ReviewedAt IS NULL THEN @Now ELSE ReviewedAt END,
    ReviewedBy = CASE WHEN ReviewedBy IS NULL THEN @AdminId ELSE ReviewedBy END,
    Notes = @Notes
WHERE Id = @Id AND IsDeleted = false;
```

**5. Soft Delete Inquiry (Admin)**
```sql
UPDATE ContactInquiries
SET IsDeleted = true,
    DeletedAt = @Now
WHERE Id = @Id;
```

---

## Validation Rules Summary

### ContactInquiry Validation

**Field-Level Validation**:
- ContactName: Required, 1-100 chars, alphanumeric + spaces/hyphens/apostrophes
- Email: Required, valid email format, max 255 chars
- OrganizationName: Required, 1-200 chars
- Message: Required, 10-2000 chars
- Status: Must be valid enum value (0, 1, or 2)

**Business-Level Validation**:
- Email must be unique per submission (prevent duplicate submissions within 1 hour)
- Message must not contain spam patterns (basic keyword filtering)
- Rate limiting: Max 5 submissions per IP per hour

**Admin-Level Validation**:
- Only PlatformAdmin can view/manage inquiries
- ReviewedBy must be valid User ID
- Status transitions must follow allowed paths

---

## Performance Considerations

### Caching Strategy
- **Organization List**: Cache for 5 minutes (low change frequency)
- **Contact Inquiries**: No caching (real-time admin dashboard)

### Indexing Strategy
- Index on `Status` for filtering by inquiry status
- Index on `SubmittedAt` for chronological sorting
- Index on `IsDeleted` for soft delete queries
- Composite index on `(IsDeleted, Status, SubmittedAt)` for admin dashboard queries

### Query Optimization
- Limit organization list to active only (reduces payload)
- Paginate contact inquiries (default 20 per page)
- Use projection (select only needed fields) for organization list

---

## Security Considerations

### Data Exposure
- **Public**: Organization name, subdomain, logo, branding colors only
- **Admin Only**: Contact inquiries (all fields)
- **Never Expose**: Tenant-specific data, user data, internal IDs

### Input Sanitization
- HTML encode all user input (ContactName, OrganizationName, Message)
- Validate email format server-side
- Prevent SQL injection via parameterized queries
- Rate limit contact form submissions

### Access Control
- Public endpoints: No authentication required
- Admin endpoints: Require PlatformAdmin role
- Soft delete: Preserve data for audit, exclude from queries

---

## Testing Considerations

### Unit Tests
- ContactInquiry entity validation
- Status transition logic
- Soft delete behavior

### Integration Tests
- Create contact inquiry end-to-end
- Retrieve active organizations
- Admin dashboard filtering and pagination

### Data Tests
- Verify indexes exist
- Verify foreign key constraints
- Verify soft delete queries exclude deleted records

---

## Future Enhancements

1. **Email Notifications**: Send email to admin on new inquiry (Option C from clarification)
2. **CMS for Landing Page**: Implement LandingPageContent entity for dynamic content
3. **Inquiry Analytics**: Track inquiry sources, conversion rates
4. **CAPTCHA Integration**: Prevent spam submissions
5. **Inquiry Assignment**: Assign inquiries to specific admins for follow-up

---

## Summary

This data model defines:
- **1 new entity**: ContactInquiry (with soft delete, status tracking, admin notes)
- **1 existing entity** (read-only): Organization/Tenant
- **1 future entity** (not implemented): LandingPageContent
- **1 enumeration**: ContactInquiryStatus
- **Clear validation rules** for all fields
- **Performance optimizations** via caching and indexing
- **Security measures** for data exposure and access control

The model supports the MVP requirements while remaining extensible for future enhancements.
