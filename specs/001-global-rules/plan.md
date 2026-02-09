# Implementation Plan: Global Rules & Constitution

**Branch**: `001-global-rules` | **Date**: 2026-02-09 | **Spec**: [spec.md](./spec.md)  
**Input**: Feature specification from `/specs/001-global-rules/spec.md`

---

## Summary

This plan establishes the **foundational infrastructure** for the Maken platform that enforces global rules defined in the Constitution. Unlike feature-specific plans, this creates the **shared building blocks** that all subsequent features will depend on:

1. **Multi-tenant infrastructure** with automatic tenant scoping
2. **Base entity patterns** with UUID primary keys and soft delete
3. **Role-based access control foundation**
4. **Onion Architecture project structure**

**Technical Approach**: Create a .NET 8 backend following Onion Architecture with shared abstractions that enforce Constitution rules automatically. No business features are implemented—only the infrastructure to support them.

---

## Technical Context

**Language/Version**: C# / .NET 8.0 LTS  
**Primary Dependencies**: 
- ASP.NET Core 8.0 (Web API)
- Entity Framework Core 8.0 (ORM)
- FluentValidation (Validation)
- MediatR (CQRS pattern for Application layer)

**Storage**: SQL Server (single database, multi-tenant with TenantId column)  
**Testing**: xUnit, FluentAssertions, Moq, TestContainers  
**Target Platform**: Linux server (Docker container), Azure App Service compatible  
**Project Type**: Web application (Backend API only in this phase)  
**Performance Goals**: 
- Tenant resolution: <50ms
- API response time: <200ms p95 for simple queries
- Concurrent users per tenant: 500+

**Constraints**: 
- All queries auto-scoped by TenantId (via EF Core global query filters)
- No hard deletes allowed at infrastructure level
- UUID generation server-side only

**Scale/Scope**: 
- 10-50 tenants at launch
- 1,000-10,000 users per tenant
- 100-500 courses per tenant

---

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Gate | Requirement | Status | Evidence |
|------|-------------|--------|----------|
| **Tenant Isolation** | All entities tenant-scoped | ✅ PASS | TenantId on all entities, EF global filters |
| **UUID Primary Keys** | All entities use GUID | ✅ PASS | Base entity with `Id = Guid.NewGuid()` |
| **Soft Delete** | No physical deletes | ✅ PASS | `IsDeleted` + `DeletedAt` on base entity |
| **Onion Architecture** | Domain → App → Infra → API | ✅ PASS | Project structure defines 4 layers |
| **Backend Logic Only** | No frontend business logic | ✅ PASS | This phase is backend-only |
| **DB Facts Only** | No stored procedures with logic | ✅ PASS | EF migrations only, no sprocs |
| **Spec-Driven** | Approved spec exists | ✅ PASS | spec.md completed and reviewed |

**Result**: All gates passed. Proceed to Phase 0.

---

## Project Structure

### Documentation (this feature)

```text
specs/001-global-rules/
├── spec.md              # Feature specification
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output (OpenAPI schemas)
│   └── base-entities.yaml
├── checklists/          # Quality validation
│   └── requirements.md
└── tasks.md             # Phase 2 output (created by /speckit.tasks)
```

### Source Code (repository root)

```text
# Onion Architecture - Backend Only

src/
├── Maken.Domain/                    # Core domain layer (innermost)
│   ├── Common/
│   │   ├── BaseEntity.cs            # UUID, soft delete, audit fields
│   │   ├── ITenantScoped.cs         # Tenant isolation interface
│   │   └── ValueObjects/
│   ├── Entities/
│   │   ├── Tenant.cs                # Company/Institute entity
│   │   ├── User.cs                  # Platform user
│   │   └── Role.cs                  # Role definitions
│   └── Enums/
│       └── RoleType.cs              # PlatformAdmin, CompanyAdmin, etc.
│
├── Maken.Application/               # Use-cases layer
│   ├── Common/
│   │   ├── Interfaces/
│   │   │   ├── IRepository.cs       # Generic repository interface
│   │   │   ├── ITenantContext.cs    # Current tenant accessor
│   │   │   └── IUnitOfWork.cs
│   │   └── Behaviors/
│   │       ├── ValidationBehavior.cs
│   │       └── TenantScopingBehavior.cs
│   └── DependencyInjection.cs
│
├── Maken.Infrastructure/            # Data & external services
│   ├── Persistence/
│   │   ├── MakenDbContext.cs        # EF Core context with global filters
│   │   ├── Configurations/          # Entity type configurations
│   │   ├── Repositories/
│   │   └── Migrations/
│   ├── Services/
│   │   └── TenantResolver.cs        # Subdomain → Tenant resolution
│   └── DependencyInjection.cs
│
└── Maken.Api/                       # Presentation layer (outermost)
    ├── Controllers/
    │   └── HealthController.cs      # Basic health check
    ├── Middleware/
    │   ├── TenantMiddleware.cs      # Resolve tenant from subdomain
    │   └── ExceptionMiddleware.cs
    ├── Program.cs
    └── appsettings.json

tests/
├── Maken.Domain.Tests/              # Unit tests for domain logic
├── Maken.Application.Tests/         # Unit tests for use-cases
├── Maken.Infrastructure.Tests/      # Integration tests with DB
└── Maken.Api.Tests/                 # API integration tests
    └── TenantIsolation.Tests.cs     # Critical: cross-tenant tests
```

**Structure Decision**: Onion Architecture with 4 projects (Domain, Application, Infrastructure, Api) plus corresponding test projects. This matches Constitution §3 requirements.

---

## Complexity Tracking

> No Constitution violations requiring justification. Standard Onion Architecture with 4 layers as specified.

---

## Phase 0: Research Summary

### Decision 1: Multi-Tenant Pattern

**Decision**: Shared database with TenantId column + EF Core global query filters

**Rationale**: 
- Single database simplifies operations for MVP
- Global query filters automatically append `WHERE TenantId = @current` to all queries
- Prevents accidental cross-tenant data access at ORM level
- Constitution allows single database (Assumption #1 in spec)

**Alternatives Considered**:
- Database-per-tenant: Rejected (operational complexity, not needed for MVP scale)
- Schema-per-tenant: Rejected (EF Core support is limited)

### Decision 2: Tenant Resolution

**Decision**: Middleware extracts subdomain from `Host` header, looks up Tenant by subdomain, stores in `HttpContext.Items`

**Rationale**:
- Constitution specifies subdomain-based routing (§4)
- Early resolution allows all downstream code to assume tenant context exists
- Cached lookup to meet <50ms requirement

**Alternatives Considered**:
- Path-based routing (`/tenant/academy/...`): Rejected (less clean URLs, not RESTful)
- Header-based: Rejected (not user-friendly for browser access)

### Decision 3: Soft Delete Implementation

**Decision**: Base entity with `IsDeleted` boolean, `DeletedAt` timestamp, `DeletedBy` user reference. EF global filter excludes deleted records.

**Rationale**:
- Constitution requires soft delete everywhere (§6)
- Global filter ensures deleted records never leak
- `IgnoreQueryFilters()` available for admin recovery operations

### Decision 4: UUID Generation

**Decision**: `Guid.NewGuid()` in entity constructor, never accept client-provided IDs

**Rationale**:
- Constitution prohibits client-generated UUIDs (PRH-007)
- Server-side generation ensures uniqueness and prevents ID enumeration attacks

### Decision 5: Role System

**Decision**: Enum-based roles stored on User entity, with claims-based authorization at API layer

**Rationale**:
- 4 fixed roles defined in Constitution (§4)
- Claims-based auth integrates well with ASP.NET Core
- No need for dynamic role creation in MVP

---

## Phase 1: Design Artifacts

### Data Model Reference

See: [data-model.md](./data-model.md)

Core entities for this phase:
- `BaseEntity` (abstract) - UUID, soft delete, audit
- `Tenant` - Company/Institute with subdomain
- `User` - Belongs to one tenant with one role
- `Role` - Enum (PlatformAdmin, CompanyAdmin, Instructor, Student)

### API Contracts Reference

See: [contracts/base-entities.yaml](./contracts/base-entities.yaml)

This phase focuses on infrastructure, so APIs are minimal:
- `GET /health` - Basic health check
- Tenant middleware (no API, internal resolution)

### Quickstart Reference

See: [quickstart.md](./quickstart.md)

Developer setup instructions for local development.

---

## Dependencies Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                        Maken.Api                            │
│                   (Controllers, Middleware)                 │
└─────────────────────────┬───────────────────────────────────┘
                          │ references
                          ▼
┌─────────────────────────────────────────────────────────────┐
│                  Maken.Application                          │
│              (Use-cases, Interfaces, Behaviors)             │
└─────────────────────────┬───────────────────────────────────┘
                          │ references
                          ▼
┌─────────────────────────────────────────────────────────────┐
│                     Maken.Domain                            │
│              (Entities, Value Objects, Enums)               │
└─────────────────────────────────────────────────────────────┘
                          ▲
                          │ implements interfaces from
┌─────────────────────────┴───────────────────────────────────┐
│                  Maken.Infrastructure                       │
│            (DbContext, Repositories, Services)              │
└─────────────────────────────────────────────────────────────┘
```

**Key Dependency Rule**: Domain has NO dependencies. Infrastructure implements Application interfaces but only Api references Infrastructure for DI registration.

---

## Risk Assessment

| Risk | Impact | Mitigation |
|------|--------|------------|
| Tenant filter bypass | Critical (data leak) | Global filter at DbContext level; test suite for cross-tenant scenarios |
| Subdomain misconfiguration | High (wrong tenant) | Strict subdomain validation; 404 for unknown subdomains |
| Soft delete data bloat | Medium (performance) | Scheduled cleanup job for old deleted records (post-MVP) |
| PlatformAdmin tenant context | Medium (logic error) | PlatformAdmin operations don't require tenant context; explicit null-tenant handling |

---

## Next Steps

1. **Run `/speckit.tasks`** to generate implementation tasks from this plan
2. Tasks will cover:
   - Solution and project setup
   - Base entity implementation
   - Tenant resolution middleware
   - EF Core context with global filters
   - Unit and integration tests

---

**Plan Version**: 1.0 | **Status**: Ready for Task Generation
