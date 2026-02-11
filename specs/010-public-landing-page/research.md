# Research: Public Landing Page

**Feature**: Public Landing Page  
**Branch**: `010-public-landing-page`  
**Date**: 2025-02-11  
**Phase**: 0 - Research & Technical Decisions

## Purpose

This document captures research findings and technical decisions for implementing a public landing page that serves as the entry point for the Maken platform.

## Research Areas

### 1. Routing Strategy for Public vs Tenant-Specific Pages

**Question**: How should the application handle routing between public landing page (no subdomain) and tenant-specific areas (with subdomain)?

**Research Findings**:
- Angular routing can handle subdomain-based routing through custom route guards
- Current implementation uses `TenantGuard` that resolves tenant from subdomain
- Public routes need to bypass tenant resolution

**Decision**: Create a public route (`/`) that bypasses the `TenantGuard`. Update the guard to allow specific public routes without tenant context.

**Rationale**: 
- Minimal changes to existing architecture
- Maintains separation between public and tenant-specific areas
- Allows future expansion of public pages if needed

**Alternatives Considered**:
- Separate subdomain for landing page (e.g., `www.maken.app`) - Rejected: Adds complexity
- Server-side routing - Rejected: Angular SPA architecture should handle routing

---

### 2. Organization Data Exposure

**Question**: What organization/tenant data should be publicly visible on the landing page?

**Research Findings**:
- Organizations have: name, subdomain, logo, branding colors, active status
- Tenant isolation is critical per constitution
- Only active/published organizations should be visible

**Decision**: Expose only: organization name, subdomain, logo URL, and branding colors (primary/secondary). Filter to active organizations only.

**Rationale**:
- Minimal data exposure maintains security
- Sufficient information for users to identify and access organizations
- Branding colors allow visual consistency

**Alternatives Considered**:
- Expose all organization data - Rejected: Security risk, unnecessary
- Expose only name and subdomain - Rejected: Poor UX without logos

---

### 3. Contact Form Storage Strategy

**Question**: How should contact inquiries be stored and managed?

**Research Findings**:
- Clarification session determined: Database storage only (no email for MVP)
- Admin dashboard needed for reviewing inquiries
- Future enhancement: Email notifications (Option C from clarification)

**Decision**: Create `ContactInquiry` entity with fields: Id, ContactName, Email, OrganizationName, Message, SubmittedAt, Status (enum: New/Reviewed/Contacted), IsDeleted, DeletedAt.

**Rationale**:
- Structured storage enables status tracking
- Soft delete support per constitution
- Extensible for future email integration
- Admin dashboard can filter by status

**Alternatives Considered**:
- Email-only (no database) - Rejected: No tracking, no audit trail
- Third-party form service - Rejected: Adds external dependency, cost

---

### 4. Landing Page Content Management

**Question**: Should landing page content (hero text, features, benefits) be hardcoded or database-driven?

**Research Findings**:
- MVP scope emphasizes simplicity
- Content changes are infrequent
- Database-driven content adds complexity

**Decision**: Hardcode landing page content in Angular component for MVP. Create `LandingPageContent` entity in data model for future CMS capability.

**Rationale**:
- Simplest approach for MVP
- Content changes can be deployed via code updates
- Future-proof: Entity defined for later CMS implementation

**Alternatives Considered**:
- Full CMS from start - Rejected: Over-engineering for MVP
- Configuration file - Rejected: Still requires deployment, no advantage over hardcoded

---

### 5. Performance Optimization

**Question**: How to achieve <2 second load time requirement?

**Research Findings**:
- Angular lazy loading for modules
- Image optimization (logos)
- API response caching
- Progressive loading strategies

**Decision**: 
1. Lazy load landing page module
2. Implement API response caching (5 minutes) for organization list
3. Use optimized image formats (WebP with fallback)
4. Implement skeleton loaders for progressive rendering

**Rationale**:
- Meets <2 second requirement
- Improves perceived performance
- Minimal backend load

**Alternatives Considered**:
- Server-side rendering (SSR) - Rejected: Adds complexity, not needed for SPA
- CDN for static assets - Deferred: Can add later if needed

---

### 6. Mobile-First Responsive Design

**Question**: What breakpoints and design patterns should be used for responsive design?

**Research Findings**:
- Constitution requires 320px-2560px support
- Mobile-first approach mandated
- Common breakpoints: 320px (mobile), 768px (tablet), 1024px (desktop), 1440px+ (large desktop)

**Decision**: Use CSS Grid and Flexbox with breakpoints at 320px, 768px, 1024px, 1440px. Implement mobile-first CSS (base styles for mobile, media queries for larger screens).

**Rationale**:
- Covers all required screen sizes
- Mobile-first ensures core functionality on smallest screens
- Grid/Flexbox provide flexible layouts

**Alternatives Considered**:
- CSS framework (Bootstrap, Tailwind) - Rejected: Adds dependency, existing project doesn't use
- Desktop-first approach - Rejected: Violates constitution

---

### 7. Admin Dashboard for Contact Inquiries

**Question**: Where should the admin dashboard for contact inquiries be located?

**Research Findings**:
- Constitution defines dual application model: Control Panel (admin) and Learning Application (student)
- Contact inquiry management is administrative function
- Only PlatformAdmin and CompanyAdmin should access

**Decision**: Place contact inquiry dashboard in Control Panel under `/admin/contact-inquiries`. Restrict access to PlatformAdmin role only (cross-tenant visibility needed).

**Rationale**:
- Aligns with dual application model
- Proper role-based access control
- Centralized admin functionality

**Alternatives Considered**:
- Separate admin portal - Rejected: Unnecessary complexity
- Email-only notifications - Rejected: Already decided database storage in clarification

---

### 8. API Endpoint Design

**Question**: What API endpoints are needed and how should they be structured?

**Research Findings**:
- RESTful conventions
- Public endpoints should not require authentication
- Admin endpoints require authentication and authorization

**Decision**: 
- `GET /api/public/organizations` - Public, no auth, returns active organizations
- `POST /api/contact-inquiries` - Public, no auth, creates inquiry
- `GET /api/contact-inquiries` - Admin only, returns all inquiries with filtering
- `PATCH /api/contact-inquiries/{id}/status` - Admin only, updates inquiry status

**Rationale**:
- Clear separation of public vs admin endpoints
- RESTful conventions
- Minimal endpoints for MVP

**Alternatives Considered**:
- GraphQL - Rejected: REST is simpler, existing project uses REST
- Single endpoint with query params - Rejected: Less clear, harder to secure

---

## Technology Stack Confirmation

### Backend
- **Language**: C# / .NET 8.0 ✅
- **Framework**: ASP.NET Core Web API ✅
- **ORM**: Entity Framework Core ✅
- **Database**: PostgreSQL (Supabase) ✅
- **Testing**: xUnit, Moq, FluentAssertions ✅

### Frontend
- **Language**: TypeScript ✅
- **Framework**: Angular 21 ✅
- **State Management**: RxJS ✅
- **Routing**: Angular Router ✅
- **Testing**: Jasmine, Karma, Vitest ✅

### Infrastructure
- **Database**: PostgreSQL via Supabase ✅
- **Storage**: Supabase Storage (for organization logos) ✅
- **Hosting**: TBD (not in scope for this feature)

---

## Dependencies

### New Dependencies
None - all required dependencies already exist in the project.

### Existing Dependencies to Use
- **Backend**: 
  - Entity Framework Core (database access)
  - MediatR (CQRS pattern)
  - FluentValidation (input validation)
  - AutoMapper (DTO mapping)
  
- **Frontend**:
  - Angular Router (routing)
  - RxJS (reactive programming)
  - Angular Forms (contact form)
  - Angular HTTP Client (API calls)

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Performance <2s not met | Low | High | Implement caching, lazy loading, image optimization |
| Mobile responsiveness issues | Medium | High | Mobile-first design, thorough testing on devices |
| Contact form spam | Medium | Medium | Add rate limiting, consider CAPTCHA in future |
| Organization logo loading slow | Medium | Low | Optimize images, implement lazy loading |
| Admin dashboard access control | Low | High | Proper role-based authorization, thorough testing |

---

## Open Questions

None - all technical decisions have been made based on research and clarification session.

---

## Next Steps

Proceed to Phase 1: Design & Contracts
- Create data-model.md
- Generate OpenAPI contract
- Create quickstart.md
- Update agent context
