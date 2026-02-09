# Feature Specification: Global Rules & Constitution

**Feature Branch**: `001-global-rules`  
**Created**: 2026-02-09  
**Status**: Draft  
**Input**: Constitution and Global Rules Specification for Maken Platform

---

## Overview

This specification defines the **global rules and constraints** that govern the entire Maken platform. These rules are non-negotiable and must be respected by all features, modules, and implementations across the system.

**Scope**: Platform-wide governance, not a single feature.

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 - AI Developer Follows Global Rules (Priority: P1)

An AI developer (Claude Code, GLM4.7, or AntiGravity) is implementing a feature. Before writing any code, the AI MUST consult the Constitution and Global Rules to ensure compliance with platform constraints.

**Why this priority**: Foundation of all development work. Without rule adherence, the entire platform integrity is compromised.

**Independent Test**: AI receives a task that would violate tenant isolation. The AI MUST refuse or request clarification rather than implement the violation.

**Acceptance Scenarios**:

1. **Given** an AI implementing a query, **When** the query would access data from another tenant, **Then** the AI MUST NOT implement the query and MUST flag the violation.
2. **Given** an AI implementing delete logic, **When** the requirement is to remove a record, **Then** the AI MUST implement soft delete (not hard delete) unless explicitly overridden by a spec exception.
3. **Given** an AI creating a new entity, **When** the entity needs an identifier, **Then** the AI MUST use UUID as the primary key.

---

### User Story 2 - Tenant Data Isolation (Priority: P1)

A CompanyAdmin manages their institute's data. They can only see, modify, and manage data belonging to their own tenant. They MUST NOT see any data from other tenants under any circumstances.

**Why this priority**: Core security and privacy requirement. Violation creates legal and trust liability.

**Independent Test**: CompanyAdmin of Tenant A attempts to access data from Tenant B. The system MUST deny access completely.

**Acceptance Scenarios**:

1. **Given** a user authenticated to Tenant A, **When** they request a resource belonging to Tenant B, **Then** the system MUST return "Not Found" or "Access Denied" (never leak that the resource exists).
2. **Given** a search or listing request, **When** data includes records from multiple tenants, **Then** the response MUST filter results to only the authenticated user's tenant.
3. **Given** a bulk operation (import, export, report), **When** executed by a tenant user, **Then** the operation scope MUST be limited to that tenant's data only.

---

### User Story 3 - Progression Rule Enforcement (Priority: P1)

A Student navigates their learning path. Lesson and course progression rules are enforced by the backend. Students cannot skip lessons, bypass exams, or access locked content regardless of frontend manipulation.

**Why this priority**: Core product differentiator. Without enforcement, Maken becomes a generic LMS.

**Independent Test**: Student attempts to access a locked lesson via direct URL or API call. Access is denied.

**Acceptance Scenarios**:

1. **Given** a lesson is locked (prerequisite incomplete), **When** a student requests that lesson's content, **Then** the system MUST deny access and return locked status.
2. **Given** a course requires exam passing, **When** a student has not passed the exam, **Then** subsequent lessons in the course MUST remain locked.
3. **Given** a course is set to "Free Flow" mode, **When** a student accesses lessons, **Then** progression rules are relaxed for that course only.

---

### Edge Cases

- **Cross-tenant API abuse**: User modifies API request to include another tenant's ID → System MUST reject.
- **Subdomain spoofing**: User attempts to access via incorrect subdomain → Tenant resolution MUST fail gracefully.
- **Deleted record access**: User requests a soft-deleted record → System MUST treat as "Not Found".
- **Free Flow scope creep**: Free Flow mode on one course MUST NOT affect progression rules on other courses.
- **Role escalation attempt**: User with Student role attempts CompanyAdmin actions → System MUST deny.

---

## Requirements *(mandatory)*

### Functional Requirements

#### Tenant Isolation Rules

- **FR-001**: System MUST resolve tenant identity from subdomain (e.g., `academy.maken.app` → Tenant: Academy).
- **FR-002**: Every database query MUST include tenant scoping automatically (no manual filtering required per query).
- **FR-003**: System MUST NOT allow cross-tenant data access under any circumstance.
- **FR-004**: Tenant isolation MUST apply to: Users, Courses, Lessons, Progress Records, Assets (PDFs, videos).
- **FR-005**: API responses MUST NOT leak existence of records from other tenants (return 404, not 403).

#### Identity & Primary Keys

- **FR-006**: All entities MUST use UUID as primary key.
- **FR-007**: UUIDs MUST be generated server-side, not client-provided.

#### Soft Delete

- **FR-008**: All entities MUST support soft delete (logical deletion, not physical removal).
- **FR-009**: Soft-deleted records MUST be excluded from normal queries by default.
- **FR-010**: Soft-deleted records MUST be restorable within 90 days of deletion; permanent purge is a post-MVP administrative function.

#### Progression Rules

- **FR-011**: Lesson unlock MUST require: (a) prior lesson completion, (b) exam passing (if exam exists).
- **FR-012**: Course unlock MUST require all prerequisite courses completed and passed.
- **FR-013**: Level unlock MUST require all courses in prior level completed and passed.
- **FR-014**: Free Flow mode MUST be a per-course configuration option.
- **FR-015**: Exam attempts MUST be unlimited.
- **FR-016**: Progression logic MUST be enforced by backend; frontend MUST NOT contain bypass logic.

#### Role & Access Control

- **FR-017**: Role system MUST include: PlatformAdmin (global), CompanyAdmin (tenant), Instructor (tenant), Student (tenant).
- **FR-018**: Users MUST belong to exactly one tenant (except PlatformAdmin).
- **FR-019**: Role-based access control MUST be enforced at API layer, not frontend.

#### Architecture Constraints

- **FR-020**: Backend MUST follow Onion Architecture: Domain → Application → Infrastructure → Presentation (API).
- **FR-021**: Domain layer MUST contain pure entities and business rules only.
- **FR-022**: Frontend MUST be stateless; all state comes from backend API.
- **FR-023**: Frontend MUST NOT contain business logic.
- **FR-024**: Database MUST store facts only; no business logic in stored procedures or triggers.

---

### Key Entities

- **Tenant (Company)**: Represents an Islamic Science Institute. Contains name, subdomain, branding configuration. All child entities are scoped to a tenant.
- **User**: Person using the platform. Belongs to exactly one tenant. Has one role. Identified by UUID.
- **Course**: Educational content container. Belongs to a tenant. Has progression rules and optional Free Flow mode.
- **Lesson**: Individual learning unit within a course. Has completion and exam requirements.
- **Progress Record**: Tracks user progress through lessons and courses. Tenant-scoped.
- **Asset**: Media files (PDFs, videos). Tenant-scoped and associated with lessons.

---

## Non-Functional Requirements

### Security

- **NFR-001**: Tenant isolation MUST be enforced at database query level, not just API layer.
- **NFR-002**: All API endpoints MUST validate tenant context before processing.
- **NFR-003**: Cross-tenant data leakage MUST be treated as a critical security incident.

### Performance

- **NFR-004**: Tenant resolution from subdomain MUST complete within 50ms.
- **NFR-005**: Progression rule checks MUST NOT add more than 50ms latency to lesson access requests.

### Auditability

- **NFR-006**: All entity changes MUST be auditable (who, when, what changed).
- **NFR-007**: Soft delete operations MUST record deletion timestamp and actor.

---

## Prohibited Actions *(Non-Negotiable)*

| Code | Prohibition | Consequence |
|------|-------------|-------------|
| **PRH-001** | Cross-tenant data visibility | Critical security violation; must block deployment |
| **PRH-002** | Skipping progression logic | Violates product core value; must block feature |
| **PRH-003** | AI assuming rules not in spec | Must reject task or request clarification |
| **PRH-004** | Business logic in database | Violates architecture; must refactor |
| **PRH-005** | Business logic in frontend | Violates architecture; must refactor |
| **PRH-006** | Hard delete without explicit spec exception | Data integrity violation |
| **PRH-007** | Client-generated UUIDs | Security risk; must generate server-side |

---

## AI Developer Notes

### For Claude Code / GLM4.7 (Backend)

1. **Always scope queries**: Every repository method MUST accept tenantId and filter by it.
2. **Never trust client input for tenant**: Tenant context comes from authenticated session, not request body.
3. **Soft delete pattern**: Use `IsDeleted` boolean + `DeletedAt` timestamp. Filter `IsDeleted = false` by default.
4. **UUID generation**: Use `Guid.NewGuid()` in .NET or equivalent.
5. **Progression checks**: Always validate access rights in Application layer use-cases, not in controllers.

### For AntiGravity / Gemini 3 Pro (Frontend)

1. **No business logic**: Do not implement progression rules, access control, or validation logic in frontend.
2. **Trust API responses**: Render exactly what the API returns; do not cache access permissions client-side.
3. **Dynamic theming**: Load theme configuration from API at runtime; do not hardcode colors or branding.
4. **Stateless**: Store no persistent state except authentication tokens.

### General AI Boundaries

- ❌ Do not assume features exist unless documented in a spec.
- ❌ Do not access the database directly; go through Application layer.
- ❌ Do not implement features that violate Constitution rules.
- ✅ Request clarification if a task seems to conflict with the Constitution.
- ✅ Reference Constitution before implementing any data access, progression, or multi-tenant logic.

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of API endpoints enforce tenant isolation (verified by security audit).
- **SC-002**: 0% of database queries allow cross-tenant data access (verified by code review).
- **SC-003**: 100% of entities use UUID primary keys (verified by schema inspection).
- **SC-004**: 100% of delete operations are soft deletes (verified by absence of DELETE statements in migrations).
- **SC-005**: Progression rules block 100% of unauthorized lesson access attempts (verified by integration tests).
- **SC-006**: AI developers can self-verify compliance using this spec (verified by developer feedback).

---

## Assumptions

1. **Single Database**: All tenants share a single database instance with logical separation.
2. **Subdomain-based Routing**: Tenant identification relies on subdomain; root domain is for PlatformAdmin only.
3. **MVP Scope**: Advanced audit logging (detailed change history) deferred to post-MVP.
4. **OAuth later**: Email/password authentication for MVP; OAuth integration planned for future iteration.

---

## Dependencies

- **Authentication System**: Must be implemented before tenant-scoped access control.
- **Tenant Management**: Must exist before any tenant-scoped feature can be built.
- **Frontend Theming API**: Required for dynamic branding at runtime.

---

## Acceptance Criteria Checklist

- [ ] Tenant isolation is enforced at query level (no cross-tenant data leakage)
- [ ] All entities use UUID primary keys
- [ ] Soft delete is implemented for all entities
- [ ] Progression rules are enforced by backend
- [ ] Role-based access control works correctly for all 4 roles
- [ ] Frontend contains no business logic
- [ ] Database contains no business logic
- [ ] AI Developer Notes are actionable and clear
- [ ] All prohibited actions are programmatically enforceable

---

**Version**: 1.0 | **Ratified**: 2026-02-09 | **Constitution Reference**: `.specify/memory/constitution.md`
