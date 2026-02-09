# Maken Constitution Specification

**Version:** 1.0  
**Date:** 2026-02-09  
**Author:** Project Management / Ziad 
---
 
## 1. Product Identity 
- **Name:** Maken
- **Type:** Product-first, multi-tenant educational platform
- **Purpose:** Provide a structured, methodology-driven learning environment for Islamic Science Institutes.
- **Not a generic LMS:** Progression rules, course prerequisites, and level sequencing are **mandatory**, not optional.
- **Target:** Any Islamic Science Institute (multi-tenant capable) with isolated data per company.
 
---
 
## 2. Core Principles
 
### I. Tenant Isolation
Each company is fully isolated:
- Users
- Courses, Lessons
- Progress records
- Assets (PDFs, videos) 
**No cross-tenant data leakage is allowed.**
 
### II. Progression Governance
Lessons and Courses must follow enforced rules:
- Lesson completion required
- Exam passing required
- Free Flow mode allowed **per Course**, optional
 
### III. Atomic & Spec-Driven Development
- No code implementation without approved spec.
- Spec-kit workflow: Constitution → Specify → Plan → Tasks
### IV. AI Coding Boundaries
- **Backend:** Claude Code + GLM4.7
- **Frontend:** AntiGravity (Gemini 3 Pro)
- AI may not assume rules not defined in spec
- No direct database access or state assumptions
---
## 3. Architectural Constraints
### Backend: Onion Architecture
```
Domain → Application → Infrastructure → Presentation (API)
```
- **Domain layer:** Contains pure entities & rules
- **Application layer:** Handles use-cases
- **Infrastructure:** Handles data & external services
- **Presentation layer:** REST API only
### Frontend: Component-Based Architecture
- Core, Shared, Feature modules
- Dynamic theming via API at runtime
- Stateless; all business logic resides in backend
---
## 4. Auth & Identity Rules
- Email/password login for MVP
- OAuth (Google) planned later
- User belongs to **one tenant only**
### Role System
| Role | Scope |
|------|-------|
| PlatformAdmin | Global |
| CompanyAdmin | Tenant-scoped |
| Instructor | Tenant-scoped |
| Student | Tenant-scoped |
- Tenant info resolved from **subdomain** (e.g., `academy.maken.app`)
---
## 5. Progression & Exams Rules
### Lesson Unlock
- Must complete lesson (watch/read)
- Must pass lesson exam (MCQ)
### Course Unlock
- All prerequisite courses completed/passed
### Level Unlock
- All courses in prior level completed/passed
### Exam Rules
- Exam attempts: **unlimited**
- Free Flow mode: optional **per course**
---
## 6. Data Management Rules
- All entities support **soft delete**
- Minimal audit/logging for MVP
- No sensitive data duplication across tenants
- Database is **facts-only**, no business logic
---
## 7. Frontend Rules
- Stateless consumer
- Renders state from backend responses only
- Respects progression & access rules from API
- Dynamic theme loaded at runtime
---
## 8. Non-Negotiable Prohibitions
| ❌ Prohibition | Rationale |
|----------------|-----------|
| No cross-tenant data visibility | Tenant isolation is absolute |
| No skipping progression logic | Core product differentiator |
| No AI assumption beyond defined rules | Spec-driven enforcement |
| No business logic in database or frontend | Onion architecture compliance |
---
## 9. Acceptance Criteria (Checklist)
- [x] Constitution approved by PM and key stakeholders
- [x] Multi-tenant identity and isolation defined
- [x] Progression rules enforced at spec level
- [x] AI coding boundaries clearly defined
- [x] Architectural constraints agreed (Backend Onion / Frontend Component)
- [x] Auth, roles, and subdomain rules documented
- [x] Exam & lesson rules defined
- [x] Soft delete and audit rules included
- [x] Frontend stateless and dynamic theming enforced
- [x] Prohibitions enforced and unambiguous
---
## Governance
  
1. This Constitution supersedes all other practices
2. Amendments require documentation, PM approval, and migration plan
3. All specs and implementations must verify compliance with this document
4. Complexity must be justified against the Constitution principles
 
---
**Version**: 1.0 | **Ratified**: 2026-02-09 | **Last Amended**: 2026-02-09
