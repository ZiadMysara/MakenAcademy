# Tasks: Global Rules & Constitution Infrastructure

**Input**: Design documents from `/specs/001-global-rules/`  
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅  
**Tests**: Included for critical tenant isolation and infrastructure validation

**Organization**: Tasks are grouped by user story to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

Based on plan.md Onion Architecture:
- **Domain**: `src/Maken.Domain/`
- **Application**: `src/Maken.Application/`
- **Infrastructure**: `src/Maken.Infrastructure/`
- **API**: `src/Maken.Api/`
- **Tests**: `tests/Maken.*.Tests/`

---

## Phase 1: Setup (Project Initialization)

**Purpose**: Create solution structure and configure projects per Onion Architecture

- [ ] T001 Create solution file `Maken.sln` at repository root
- [ ] T002 [P] Create `src/Maken.Domain/Maken.Domain.csproj` (class library, no dependencies)
- [ ] T003 [P] Create `src/Maken.Application/Maken.Application.csproj` (class library, references Domain)
- [ ] T004 [P] Create `src/Maken.Infrastructure/Maken.Infrastructure.csproj` (class library, references Application)
- [ ] T005 [P] Create `src/Maken.Api/Maken.Api.csproj` (web API, references Application and Infrastructure)
- [ ] T006 [P] Create `tests/Maken.Domain.Tests/Maken.Domain.Tests.csproj` (xUnit)
- [ ] T007 [P] Create `tests/Maken.Application.Tests/Maken.Application.Tests.csproj` (xUnit)
- [ ] T008 [P] Create `tests/Maken.Infrastructure.Tests/Maken.Infrastructure.Tests.csproj` (xUnit)
- [ ] T009 [P] Create `tests/Maken.Api.Tests/Maken.Api.Tests.csproj` (xUnit)
- [ ] T010 Add NuGet packages: EF Core 8, FluentValidation, MediatR per plan.md
- [ ] T011 [P] Create `.editorconfig` with C# coding standards
- [ ] T012 [P] Create `Directory.Build.props` with shared project settings

**Checkpoint**: Solution builds with empty projects

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story implementation

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

### Base Entity Pattern

- [ ] T013 Create `src/Maken.Domain/Common/BaseEntity.cs` with UUID, soft delete, audit fields per data-model.md
- [ ] T014 Create `src/Maken.Domain/Common/ITenantScoped.cs` interface with TenantId property
- [ ] T015 Create `src/Maken.Domain/Common/TenantScopedEntity.cs` abstract class extending BaseEntity

### Enums

- [ ] T016 Create `src/Maken.Domain/Enums/RoleType.cs` enum (PlatformAdmin=0, CompanyAdmin=1, Instructor=2, Student=3)

### Core Entities

- [ ] T017 [P] Create `src/Maken.Domain/Entities/Tenant.cs` per data-model.md
- [ ] T018 [P] Create `src/Maken.Domain/Entities/User.cs` per data-model.md

### Application Layer Interfaces

- [ ] T019 Create `src/Maken.Application/Common/Interfaces/ITenantContext.cs` for current tenant accessor
- [ ] T020 [P] Create `src/Maken.Application/Common/Interfaces/IRepository.cs` generic repository interface
- [ ] T021 [P] Create `src/Maken.Application/Common/Interfaces/IUnitOfWork.cs` interface
- [ ] T022 Create `src/Maken.Application/DependencyInjection.cs` for service registration

### Database Context

- [ ] T023 Create `src/Maken.Infrastructure/Persistence/MakenDbContext.cs` with global query filters per research.md
- [ ] T024 [P] Create `src/Maken.Infrastructure/Persistence/Configurations/TenantConfiguration.cs` (EF type config)
- [ ] T025 [P] Create `src/Maken.Infrastructure/Persistence/Configurations/UserConfiguration.cs` (EF type config)
- [ ] T026 Create `src/Maken.Infrastructure/DependencyInjection.cs` for service registration

### API Infrastructure

- [ ] T027 Create `src/Maken.Api/Program.cs` with DI setup, middleware pipeline
- [ ] T028 [P] Create `src/Maken.Api/appsettings.json` with connection string template
- [ ] T029 [P] Create `src/Maken.Api/appsettings.Development.json` with local settings

### Initial Migration

- [ ] T030 Create initial EF Core migration for Tenant and User tables

**Checkpoint**: Foundation ready - database can be created, API starts without errors

---

## Phase 3: User Story 1 - AI Developer Follows Global Rules (Priority: P1) 🎯 MVP

**Goal**: Ensure all queries are tenant-scoped automatically and soft delete is enforced at infrastructure level

**Independent Test**: Query for entity without tenant context → Exception thrown. Delete operation → Sets IsDeleted flag only.

### Tests for User Story 1

- [ ] T031 [P] [US1] Unit test: BaseEntity generates UUID on construction in `tests/Maken.Domain.Tests/Common/BaseEntityTests.cs`
- [ ] T032 [P] [US1] Unit test: SoftDelete sets IsDeleted and DeletedAt in `tests/Maken.Domain.Tests/Common/BaseEntityTests.cs`
- [ ] T033 [P] [US1] Integration test: Global filter excludes deleted records in `tests/Maken.Infrastructure.Tests/Persistence/GlobalFilterTests.cs`
- [ ] T034 [P] [US1] Integration test: Global filter scopes by TenantId in `tests/Maken.Infrastructure.Tests/Persistence/TenantScopingTests.cs`

### Implementation for User Story 1

- [ ] T035 [US1] Implement soft delete interception in `src/Maken.Infrastructure/Persistence/MakenDbContext.cs` (override SaveChangesAsync)
- [ ] T036 [US1] Configure global query filter for IsDeleted in DbContext
- [ ] T037 [US1] Configure global query filter for TenantId in DbContext using ITenantContext
- [ ] T038 [US1] Implement `src/Maken.Infrastructure/Services/TenantContext.cs` (resolves from HttpContext)
- [ ] T039 [US1] Add XML documentation comments to BaseEntity explaining Constitution requirements

**Checkpoint**: Queries auto-filter by tenant and soft delete; AI developers can reference patterns

---

## Phase 4: User Story 2 - Tenant Data Isolation (Priority: P1)

**Goal**: Resolve tenant from subdomain and inject into all requests; reject requests for unknown tenants

**Independent Test**: Request to `academy.maken.app` resolves correct tenant; request to `unknown.maken.app` returns 404.

### Tests for User Story 2

- [ ] T040 [P] [US2] Unit test: Subdomain extraction from Host header in `tests/Maken.Api.Tests/Middleware/TenantMiddlewareTests.cs`
- [ ] T041 [P] [US2] Integration test: Valid subdomain → tenant resolved in `tests/Maken.Api.Tests/Middleware/TenantMiddlewareTests.cs`
- [ ] T042 [P] [US2] Integration test: Invalid subdomain → 404 response in `tests/Maken.Api.Tests/Middleware/TenantMiddlewareTests.cs`
- [ ] T043 [P] [US2] Integration test: Cross-tenant query blocked in `tests/Maken.Infrastructure.Tests/Persistence/TenantIsolationTests.cs`

### Implementation for User Story 2

- [ ] T044 [US2] Create `src/Maken.Infrastructure/Services/TenantResolver.cs` (subdomain lookup with cache per research.md)
- [ ] T045 [US2] Create `src/Maken.Api/Middleware/TenantMiddleware.cs` (extract subdomain, resolve tenant, set HttpContext)
- [ ] T046 [US2] Register TenantMiddleware in `src/Maken.Api/Program.cs` pipeline
- [ ] T047 [US2] Create `src/Maken.Application/Common/Interfaces/ITenantResolver.cs` interface
- [ ] T048 [US2] Add reserved subdomain validation (www, api, admin, app) in TenantResolver
- [ ] T049 [US2] Implement memory cache for tenant lookup (5 min TTL per research.md)

**Checkpoint**: Requests are tenant-scoped; unknown subdomains return 404

---

## Phase 5: User Story 3 - Progression Rule Enforcement (Priority: P1)

**Goal**: Establish the infrastructure pattern for progression rules (actual implementation in Course/Lesson specs)

**Independent Test**: Placeholder service that demonstrates the progression check pattern is callable from API layer.

### Tests for User Story 3

- [ ] T050 [P] [US3] Unit test: RoleType enum values match Constitution in `tests/Maken.Domain.Tests/Enums/RoleTypeTests.cs`
- [ ] T051 [P] [US3] Unit test: User.TenantId null only for PlatformAdmin in `tests/Maken.Domain.Tests/Entities/UserTests.cs`

### Implementation for User Story 3

- [ ] T052 [US3] Create `src/Maken.Domain/Common/IProgressionRule.cs` interface (placeholder for future Course/Lesson rules)
- [ ] T053 [US3] Add validation in User entity: TenantId required except PlatformAdmin in `src/Maken.Domain/Entities/User.cs`
- [ ] T054 [US3] Create `src/Maken.Application/Common/Behaviors/ValidationBehavior.cs` for MediatR pipeline
- [ ] T055 [US3] Document progression rule pattern in code comments for AI developers
- [ ] T056 [US3] Add `[Authorize]` attribute configuration and role-based policies in `src/Maken.Api/Program.cs`

**Checkpoint**: Role enforcement works; progression pattern documented for future features

---

## Phase 6: Health Check & API Contracts

**Purpose**: Implement API health check per contracts/base-entities.yaml

- [ ] T057 [P] Create `src/Maken.Api/Controllers/HealthController.cs` per OpenAPI contract
- [ ] T058 [P] Create `src/Maken.Api/Models/HealthResponse.cs` DTO matching OpenAPI schema
- [ ] T059 [P] Create `src/Maken.Api/Models/ApiResponse.cs` generic response wrapper
- [ ] T060 [P] Create `src/Maken.Api/Models/ApiError.cs` error model
- [ ] T061 Implement database health check in HealthController
- [ ] T062 [P] Integration test: Health endpoint returns healthy when DB connected in `tests/Maken.Api.Tests/Controllers/HealthControllerTests.cs`

**Checkpoint**: Health endpoint works per OpenAPI contract

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect all user stories

- [ ] T063 [P] Create `src/Maken.Api/Middleware/ExceptionMiddleware.cs` for global error handling
- [ ] T064 [P] Add Swagger/OpenAPI documentation to API with XML comments
- [ ] T065 Create seed data script for demo tenant and PlatformAdmin user
- [ ] T066 [P] Add README.md at repository root with project overview
- [ ] T067 Run quickstart.md validation steps manually
- [ ] T068 Code review: Verify all entities inherit from BaseEntity or TenantScopedEntity
- [ ] T069 Code review: Verify no hard DELETE statements in codebase
- [ ] T070 Code review: Verify no business logic in DbContext beyond filters

---

## Dependencies & Execution Order

### Phase Dependencies

```
Phase 1: Setup
    │
    ▼
Phase 2: Foundational (BLOCKS ALL USER STORIES)
    │
    ├─────────────────┬─────────────────┬─────────────────┐
    ▼                 ▼                 ▼                 ▼
Phase 3: US1      Phase 4: US2      Phase 5: US3      Phase 6: Health
(Global Rules)    (Tenant Isolation) (Progression)     (API Contract)
    │                 │                 │                 │
    └─────────────────┴─────────────────┴─────────────────┘
                              │
                              ▼
                      Phase 7: Polish
```

### User Story Dependencies

| Story | Depends On | Can Start After |
|-------|------------|-----------------|
| US1 (Global Rules) | Foundational | Phase 2 complete |
| US2 (Tenant Isolation) | Foundational + US1 | T037 complete (needs global filter) |
| US3 (Progression) | Foundational | Phase 2 complete |
| Health API | Foundational | Phase 2 complete |

### Within Each Story

1. Tests FIRST → assert they FAIL
2. Domain entities/interfaces
3. Application services
4. Infrastructure implementations
5. API endpoints
6. Run tests → assert they PASS

---

## Parallel Execution Examples

### Phase 2 Parallel Group 1 (Entities)

```
Parallel Tasks:
- T017 Create Tenant entity
- T018 Create User entity
```

### Phase 2 Parallel Group 2 (Interfaces)

```
Parallel Tasks:
- T020 IRepository interface
- T021 IUnitOfWork interface
```

### Phase 3 (US1) Parallel Group

```
Parallel Tasks:
- T031 BaseEntity UUID test
- T032 SoftDelete test
- T033 Global filter test
- T034 Tenant scoping test
```

### Phase 4 (US2) Parallel Group

```
Parallel Tasks:
- T040 Subdomain extraction test
- T041 Valid subdomain test
- T042 Invalid subdomain test
- T043 Cross-tenant query test
```

---

## Implementation Strategy

### MVP First (Phase 1-3 Only)

1. Complete Phase 1: Setup (~2 hours)
2. Complete Phase 2: Foundational (~4 hours)
3. Complete Phase 3: User Story 1 - Global Rules (~3 hours)
4. **STOP and VALIDATE**: Run tests, verify soft delete and UUID generation
5. This delivers: Onion Architecture + Base Entity Pattern + Global Filters

### Full Infrastructure

1. MVP (Phases 1-3) → Foundation works
2. Add Phase 4: Tenant Resolution → Multi-tenant works
3. Add Phase 5: Progression Pattern → Ready for Course/Lesson features
4. Add Phase 6: Health API → Production-ready monitoring
5. Add Phase 7: Polish → Documentation and hardening

### Time Estimates

| Phase | Estimated Time | Cumulative |
|-------|---------------|------------|
| Phase 1: Setup | 2 hours | 2 hours |
| Phase 2: Foundational | 4 hours | 6 hours |
| Phase 3: US1 | 3 hours | 9 hours |
| Phase 4: US2 | 3 hours | 12 hours |
| Phase 5: US3 | 2 hours | 14 hours |
| Phase 6: Health | 1 hour | 15 hours |
| Phase 7: Polish | 2 hours | 17 hours |

---

## Task Summary

| Category | Count |
|----------|-------|
| **Total Tasks** | 70 |
| **Phase 1 (Setup)** | 12 |
| **Phase 2 (Foundational)** | 18 |
| **Phase 3 (US1)** | 9 |
| **Phase 4 (US2)** | 10 |
| **Phase 5 (US3)** | 7 |
| **Phase 6 (Health)** | 6 |
| **Phase 7 (Polish)** | 8 |
| **Parallel Opportunities** | 32 tasks marked [P] |

---

## Notes

- All tasks include exact file paths per Onion Architecture from plan.md
- Tests are included for critical infrastructure (tenant isolation, soft delete)
- Each user story is independently testable after foundational phase
- [P] tasks can run in parallel within their phase
- Commit after each task or logical group
- Stop at any checkpoint to validate before proceeding
