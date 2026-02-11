# Implementation Plan: Public Landing Page

**Branch**: `010-public-landing-page` | **Date**: 2025-02-11 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/010-public-landing-page/spec.md`

## Summary

Create a public landing page that serves as the entry point for the Maken platform when users visit the root domain without a subdomain. The landing page will showcase available organizations, provide platform information, and offer a contact form for new organizations. This solves the current "tenant not resolved" error and provides a professional entry point for the platform.

**Technical Approach**: Implement a dual-surface solution with a public Angular component for the landing page (frontend) and ASP.NET Core API endpoints for fetching organizations and storing contact inquiries (backend). The landing page will be accessible without authentication and will not require tenant context resolution.

## Technical Context

**Language/Version**: 
- Backend: C# / .NET 8.0
- Frontend: TypeScript / Angular 21

**Primary Dependencies**: 
- Backend: ASP.NET Core Web API, Entity Framework Core, PostgreSQL (via Supabase)
- Frontend: Angular 21, RxJS, Angular Router

**Storage**: PostgreSQL (Supabase) - ContactInquiry entity, existing Tenant/Organization table

**Testing**: 
- Backend: xUnit, Moq, FluentAssertions
- Frontend: Jasmine, Karma, Vitest

**Target Platform**: Web application (desktop, tablet, mobile browsers)

**Project Type**: Web application with separate frontend and backend

**Performance Goals**: 
- Landing page load time: <2 seconds on standard broadband
- Organization list fetch: <500ms
- Contact form submission: <1 second
- Responsive design: 320px-2560px screen widths

**Constraints**: 
- Must work without authentication or tenant context
- Must not leak tenant-specific data
- Must handle zero organizations gracefully
- Must be mobile-first responsive

**Scale/Scope**: 
- Expected organizations: 10-100 initially
- Contact inquiries: ~10-50 per month initially
- Concurrent visitors: 100-500

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Compliance Review

✅ **Tenant Isolation**: Landing page is public and does not require tenant context. Organization list only shows active/published tenants without exposing sensitive data.

✅ **Progression Governance**: Not applicable - landing page is pre-authentication entry point.

✅ **Atomic & Spec-Driven Development**: Following spec-kit workflow (Constitution → Specify → Clarify → Plan → Tasks).

✅ **AI Coding Boundaries**: Backend (Claude/GLM4.7), Frontend (AntiGravity/Gemini 3 Pro) as specified.

✅ **Tech Stack Compliance**: 
- Frontend: Angular 21 ✅
- Backend: ASP.NET Core Web API (.NET 8) ✅
- Database: PostgreSQL (Supabase) ✅

✅ **Onion Architecture**: Backend follows Domain → Application → Infrastructure → API layers.

✅ **Frontend Component-Based**: Landing page will be a standalone Angular component with proper separation.

✅ **Auth & Identity**: Landing page is public (no auth required). Contact form does not require authentication.

✅ **Data Management**: ContactInquiry entity will support soft delete as per constitution.

✅ **Frontend Stateless**: Landing page consumes backend API responses only, no business logic in frontend.

✅ **Dual Application Model**: Landing page is separate from both Control Panel and Learning Application - serves as entry point to both.

✅ **Responsive & Mobile**: Mobile-first design required (320px-2560px).

✅ **MVP Scope Lock**: No certificates, no OAuth (Google) - using simple contact form only.

✅ **Simplicity Constraint**: Simplest approach - static landing page with API-driven organization list and basic contact form.

### Gate Status: ✅ PASSED

No constitution violations. Feature aligns with all architectural constraints and principles.

## Project Structure

### Documentation (this feature)

```text
specs/010-public-landing-page/
├── plan.md              # This file
├── spec.md              # Feature specification (completed)
├── research.md          # Phase 0 output (to be generated)
├── data-model.md        # Phase 1 output (to be generated)
├── quickstart.md        # Phase 1 output (to be generated)
├── contracts/           # Phase 1 output (to be generated)
│   └── openapi.yaml     # API contract for landing page endpoints
└── tasks.md             # Phase 2 output (/speckit.tasks command)
```

### Source Code (repository root)

```text
backend/
├── src/
│   ├── Maken.Domain/
│   │   └── Entities/
│   │       └── ContactInquiry.cs          # New entity
│   ├── Maken.Application/
│   │   ├── Commands/
│   │   │   └── ContactInquiries/
│   │   │       ├── CreateContactInquiryCommand.cs
│   │   │       └── CreateContactInquiryCommandHandler.cs
│   │   ├── Queries/
│   │   │   ├── Organizations/
│   │   │   │   ├── GetPublicOrganizationsQuery.cs
│   │   │   │   └── GetPublicOrganizationsQueryHandler.cs
│   │   │   └── ContactInquiries/
│   │   │       ├── GetContactInquiriesQuery.cs
│   │   │       └── GetContactInquiriesQueryHandler.cs
│   │   └── DTOs/
│   │       ├── PublicOrganizationDto.cs
│   │       └── ContactInquiryDto.cs
│   ├── Maken.Infrastructure/
│   │   ├── Persistence/
│   │   │   ├── Configurations/
│   │   │   │   └── ContactInquiryConfiguration.cs
│   │   │   └── Repositories/
│   │   │       └── ContactInquiryRepository.cs
│   │   └── Migrations/
│   │       └── [timestamp]_AddContactInquiryEntity.cs
│   └── Maken.Api/
│       ├── Controllers/
│       │   ├── PublicController.cs         # New: GET /api/public/organizations
│       │   └── ContactInquiriesController.cs # New: POST /api/contact-inquiries, GET /api/contact-inquiries (admin)
│       └── DTOs/
│           ├── Requests/
│           │   └── CreateContactInquiryRequest.cs
│           └── Responses/
│               ├── PublicOrganizationResponse.cs
│               └── ContactInquiryResponse.cs
└── tests/
    ├── Maken.Domain.Tests/
    │   └── Entities/
    │       └── ContactInquiryTests.cs
    ├── Maken.Application.Tests/
    │   ├── Commands/
    │   │   └── CreateContactInquiryCommandTests.cs
    │   └── Queries/
    │       ├── GetPublicOrganizationsQueryTests.cs
    │       └── GetContactInquiriesQueryTests.cs
    └── Maken.Api.Tests/
        └── Controllers/
            ├── PublicControllerTests.cs
            └── ContactInquiriesControllerTests.cs

frontend/
├── src/
│   └── app/
│       ├── public/                         # New: Public landing page module
│       │   ├── landing-page/
│       │   │   ├── landing-page.component.ts
│       │   │   ├── landing-page.component.html
│       │   │   ├── landing-page.component.css
│       │   │   └── landing-page.component.spec.ts
│       │   ├── components/
│       │   │   ├── hero-section/
│       │   │   ├── organization-showcase/
│       │   │   ├── features-section/
│       │   │   ├── how-it-works-section/
│       │   │   └── contact-form/
│       │   ├── services/
│       │   │   ├── public-api.service.ts
│       │   │   └── public-api.service.spec.ts
│       │   └── public.routes.ts
│       ├── control-panel/
│       │   └── contact-inquiries/          # New: Admin dashboard for inquiries
│       │       ├── contact-inquiry-list/
│       │       └── contact-inquiry-detail/
│       ├── app.routes.ts                   # Update: Add public route
│       └── core/
│           └── guards/
│               └── tenant.guard.ts         # Update: Bypass for public routes
└── tests/
    └── public/
        ├── landing-page.component.spec.ts
        └── public-api.service.spec.ts
```

**Structure Decision**: Web application structure (Option 2) selected. The feature adds:
1. **Backend**: New ContactInquiry entity, public organizations endpoint, contact inquiry management
2. **Frontend**: New public module with landing page component and sub-components, admin dashboard for inquiries
3. **Routing**: Public route (`/`) that bypasses tenant guard, admin route for inquiry management

## Complexity Tracking

> **No constitution violations detected. This section is not applicable.**

The feature follows all constitutional constraints:
- Uses existing tech stack (Angular 21, ASP.NET Core 8, PostgreSQL)
- Follows Onion Architecture in backend
- Maintains tenant isolation (public data only)
- Frontend remains stateless
- Implements soft delete for ContactInquiry entity
- Mobile-first responsive design
- Simplest approach for MVP (no email integration, database storage only)
