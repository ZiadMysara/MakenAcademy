# Data Model: Global Rules Foundation

**Feature**: 001-global-rules  
**Date**: 2026-02-09  
**Status**: Complete

---

## Overview

This document defines the **foundational entities** for the Maken platform. These entities establish the patterns that all future entities must follow:

- UUID primary keys
- Soft delete support
- Tenant scoping (where applicable)
- Audit fields

---

## Base Entity (Abstract)

All entities inherit from this base class.

### BaseEntity

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| `Id` | Guid (UUID) | PK, Required, Generated | Unique identifier, server-generated |
| `CreatedAt` | DateTime | Required | UTC timestamp of creation |
| `CreatedBy` | Guid? | Optional | User ID who created the record |
| `UpdatedAt` | DateTime? | Optional | UTC timestamp of last update |
| `UpdatedBy` | Guid? | Optional | User ID who last updated |
| `IsDeleted` | Boolean | Required, Default: false | Soft delete flag |
| `DeletedAt` | DateTime? | Optional | UTC timestamp of deletion |
| `DeletedBy` | Guid? | Optional | User ID who deleted |

**Invariants**:
- `Id` is set at construction, never changes
- `IsDeleted = true` requires `DeletedAt` to be set
- All timestamps are UTC

---

## Tenant-Scoped Base Entity (Abstract)

Entities that belong to a tenant inherit from this class.

### TenantScopedEntity : BaseEntity

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| `TenantId` | Guid | Required, FK→Tenant | Owner tenant |

**Invariants**:
- `TenantId` is set at construction, never changes
- Global query filter: `WHERE TenantId = @currentTenantId AND IsDeleted = false`

---

## Core Entities

### Tenant

Represents an Islamic Science Institute (Company).

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| `Id` | Guid | PK (inherited) | Unique identifier |
| `Name` | String | Required, Max 200 | Display name |
| `Subdomain` | String | Required, Max 50, Unique | URL subdomain (e.g., "academy") |
| `LogoUrl` | String? | Optional, Max 500 | Branding logo URL |
| `PrimaryColor` | String? | Optional, Max 7 | Hex color for theming |
| `SecondaryColor` | String? | Optional, Max 7 | Hex color for theming |
| `IsActive` | Boolean | Required, Default: true | Tenant activation status |
| `CreatedAt` | DateTime | Required (inherited) | — |
| `IsDeleted` | Boolean | Required (inherited) | — |

**Relationships**:
- One-to-many: Tenant → Users
- One-to-many: Tenant → Courses (future)
- One-to-many: Tenant → Assets (future)

**Business Rules**:
- `Subdomain` must be lowercase alphanumeric with optional hyphens
- `Subdomain` cannot be: "www", "api", "admin", "app" (reserved)
- Inactive tenant users cannot log in

---

### User

Represents a person using the platform.

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| `Id` | Guid | PK (inherited) | Unique identifier |
| `TenantId` | Guid? | FK→Tenant, Nullable | Owner tenant (null for PlatformAdmin) |
| `Email` | String | Required, Max 256, Unique per Tenant | Login email |
| `PasswordHash` | String | Required, Max 256 | Hashed password (bcrypt) |
| `FirstName` | String | Required, Max 100 | — |
| `LastName` | String | Required, Max 100 | — |
| `Role` | RoleType (Enum) | Required | User's role |
| `IsActive` | Boolean | Required, Default: true | Account status |
| `LastLoginAt` | DateTime? | Optional | Last successful login |
| `CreatedAt` | DateTime | Required (inherited) | — |
| `IsDeleted` | Boolean | Required (inherited) | — |

**Relationships**:
- Many-to-one: User → Tenant (optional for PlatformAdmin)
- One-to-many: User → ProgressRecords (future)

**Business Rules**:
- `TenantId` is null ONLY for PlatformAdmin role
- Email must be unique within a tenant (not globally)
- PlatformAdmin cannot belong to any tenant
- Only one role per user (no multi-role)

---

### RoleType (Enum)

Defines the fixed roles in the system.

| Value | Name | Scope | Description |
|-------|------|-------|-------------|
| 0 | PlatformAdmin | Global | Manages all tenants, no tenant membership |
| 1 | CompanyAdmin | Tenant | Manages their tenant's settings, users, courses |
| 2 | Instructor | Tenant | Creates and manages courses, views student progress |
| 3 | Student | Tenant | Consumes courses, takes exams |

**Business Rules**:
- Defined in Constitution §4
- Cannot be extended without Constitution amendment
- Higher value = lower privilege (for ordering)

---

## Entity Relationships Diagram

```
┌─────────────────────────────────────────────────────────┐
│                        Tenant                           │
│ ─────────────────────────────────────────────────────── │
│ Id: Guid (PK)                                          │
│ Name: string                                           │
│ Subdomain: string (unique)                             │
│ LogoUrl: string?                                       │
│ PrimaryColor: string?                                  │
│ SecondaryColor: string?                                │
│ IsActive: bool                                         │
│ [+ BaseEntity fields]                                  │
└──────────────────────────┬──────────────────────────────┘
                           │
                           │ 1:N
                           ▼
┌─────────────────────────────────────────────────────────┐
│                         User                            │
│ ─────────────────────────────────────────────────────── │
│ Id: Guid (PK)                                          │
│ TenantId: Guid? (FK → Tenant, null for PlatformAdmin)  │
│ Email: string                                          │
│ PasswordHash: string                                   │
│ FirstName: string                                      │
│ LastName: string                                       │
│ Role: RoleType (enum)                                  │
│ IsActive: bool                                         │
│ LastLoginAt: DateTime?                                 │
│ [+ BaseEntity fields]                                  │
└─────────────────────────────────────────────────────────┘
```

---

## Validation Rules

### Tenant Validation

| Field | Rule |
|-------|------|
| Name | Required, 1-200 characters |
| Subdomain | Required, 1-50 characters, lowercase alphanumeric + hyphen, no reserved words |

### User Validation

| Field | Rule |
|-------|------|
| Email | Required, valid email format, max 256 characters |
| Password (input) | Required, min 8 characters, at least 1 number, 1 uppercase |
| FirstName | Required, 1-100 characters |
| LastName | Required, 1-100 characters |
| Role | Must be valid RoleType enum value |
| TenantId | Required if Role != PlatformAdmin; null if Role == PlatformAdmin |

---

## State Transitions

### User States

```
                    ┌─────────┐
                    │ Created │
                    └────┬────┘
                         │
           ┌─────────────┼─────────────┐
           ▼             ▼             ▼
       ┌───────┐    ┌──────────┐   ┌──────────┐
       │ Active│◄──►│ Inactive │──►│ Deleted  │
       └───────┘    └──────────┘   └──────────┘
           │                            ▲
           └────────────────────────────┘
```

- **Created → Active**: Default state on creation
- **Active ↔ Inactive**: Admin toggles `IsActive`
- **Any → Deleted**: Soft delete sets `IsDeleted = true`

### Tenant States

```
       ┌────────┐         ┌──────────┐
       │ Active │ ◄─────► │ Inactive │
       └────┬───┘         └──────────┘
            │                   │
            ▼                   ▼
       ┌─────────┐
       │ Deleted │
       └─────────┘
```

- **Inactive Tenant**: All users cannot log in; data remains
- **Deleted Tenant**: Soft deleted; recoverable

---

## Indexes

### Tenant

| Index Name | Columns | Type | Purpose |
|------------|---------|------|---------|
| PK_Tenant | Id | Clustered | Primary key |
| IX_Tenant_Subdomain | Subdomain | Unique, Nonclustered | Subdomain lookup |
| IX_Tenant_IsDeleted | IsDeleted | Nonclustered | Filter for soft delete |

### User

| Index Name | Columns | Type | Purpose |
|------------|---------|------|---------|
| PK_User | Id | Clustered | Primary key |
| IX_User_TenantId | TenantId | Nonclustered | Tenant filtering |
| IX_User_Email_TenantId | Email, TenantId | Unique, Nonclustered | Unique email per tenant |
| IX_User_IsDeleted | IsDeleted | Nonclustered | Filter for soft delete |

---

## Future Entities (Placeholders)

These entities will be defined in their respective feature specs:

- **Course**: Tenant-scoped, has progression rules
- **Lesson**: Belongs to Course, has exam requirements
- **Exam**: MCQ exam for lessons
- **ProgressRecord**: User progress through lessons/courses
- **Asset**: PDFs, videos attached to lessons

All future entities MUST:
1. Inherit from `BaseEntity` or `TenantScopedEntity`
2. Use UUID primary key
3. Support soft delete
4. Include audit fields

---

**Data Model Status**: Complete  
**Proceed to**: API Contracts
