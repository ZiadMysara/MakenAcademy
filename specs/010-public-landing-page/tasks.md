# Tasks: Public Landing Page

**Input**: Design documents from `/specs/010-public-landing-page/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/openapi.yaml, quickstart.md

**Tests**: Tests are NOT explicitly requested in the specification, so test tasks are excluded from this implementation plan.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3, US4)
- Include exact file paths in descriptions

## Path Conventions

- **Backend**: `src/Maken.Domain/`, `src/Maken.Application/`, `src/Maken.Infrastructure/`, `src/Maken.Api/`
- **Frontend**: `frontend/src/app/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

- [ ] T001 Verify .NET 8.0 SDK and Angular CLI 21 are installed
- [ ] T002 Create feature branch `010-public-landing-page` from main
- [ ] T003 [P] Review constitution compliance from plan.md (already passed)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [ ] T004 Create ContactInquiry entity in `src/Maken.Domain/Entities/ContactInquiry.cs` with all fields (Id, ContactName, Email, OrganizationName, Message, Status, SubmittedAt, ReviewedAt, ReviewedBy, Notes, IsDeleted, DeletedAt)
- [ ] T005 Create ContactInquiryStatus enum in `src/Maken.Domain/Entities/ContactInquiry.cs` (New=0, Reviewed=1, Contacted=2)
- [ ] T006 [P] Create ContactInquiry EF Core configuration in `src/Maken.Infrastructure/Persistence/Configurations/ContactInquiryConfiguration.cs`
- [ ] T007 Add ContactInquiry DbSet to ApplicationDbContext in `src/Maken.Infrastructure/Persistence/ApplicationDbContext.cs`
- [ ] T008 Create and apply database migration for ContactInquiry entity using `dotnet ef migrations add AddContactInquiryEntity`
- [ ] T009 [P] Create PublicOrganizationDto in `src/Maken.Application/DTOs/PublicOrganizationDto.cs`
- [ ] T010 [P] Create ContactInquiryDto in `src/Maken.Application/DTOs/ContactInquiryDto.cs`
- [ ] T011 [P] Create CreateContactInquiryRequest in `src/Maken.Api/DTOs/Requests/CreateContactInquiryRequest.cs`
- [ ] T012 [P] Create UpdateContactInquiryStatusRequest in `src/Maken.Api/DTOs/Requests/UpdateContactInquiryStatusRequest.cs`
- [ ] T013 [P] Create PublicOrganizationResponse in `src/Maken.Api/DTOs/Responses/PublicOrganizationResponse.cs`
- [ ] T014 [P] Create ContactInquiryResponse in `src/Maken.Api/DTOs/Responses/ContactInquiryResponse.cs`
- [ ] T015 [P] Create ContactInquiryListResponse in `src/Maken.Api/DTOs/Responses/ContactInquiryListResponse.cs`

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Platform Discovery (Priority: P1) 🎯 MVP

**Goal**: Visitors can access the landing page and view available organizations without seeing "tenant not resolved" error

**Independent Test**: Navigate to `http://localhost:4200/` and verify landing page loads with platform branding and organization list

### Backend Implementation for User Story 1

- [ ] T016 [P] [US1] Create GetPublicOrganizationsQuery in `src/Maken.Application/Queries/Organizations/GetPublicOrganizationsQuery.cs`
- [ ] T017 [US1] Create GetPublicOrganizationsQueryHandler in `src/Maken.Application/Queries/Organizations/GetPublicOrganizationsQueryHandler.cs` (filters IsActive=true, IsDeleted=false, maps to PublicOrganizationDto)
- [ ] T018 [US1] Create PublicController in `src/Maken.Api/Controllers/PublicController.cs` with [AllowAnonymous] attribute
- [ ] T019 [US1] Implement GET /api/public/organizations endpoint in PublicController with 5-minute response caching
- [ ] T020 [US1] Add AutoMapper profile for Tenant → PublicOrganizationDto mapping in `src/Maken.Application/Mappings/OrganizationMappingProfile.cs`

### Frontend Implementation for User Story 1

- [ ] T021 [P] [US1] Create public module structure: `frontend/src/app/public/`
- [ ] T022 [P] [US1] Generate landing-page component in `frontend/src/app/public/landing-page/`
- [ ] T023 [P] [US1] Generate hero-section component in `frontend/src/app/public/components/hero-section/`
- [ ] T024 [P] [US1] Generate organization-showcase component in `frontend/src/app/public/components/organization-showcase/`
- [ ] T025 [P] [US1] Generate features-section component in `frontend/src/app/public/components/features-section/`
- [ ] T026 [P] [US1] Generate how-it-works-section component in `frontend/src/app/public/components/how-it-works-section/`
- [ ] T027 [P] [US1] Create PublicApiService in `frontend/src/app/public/services/public-api.service.ts`
- [ ] T028 [US1] Implement getOrganizations() method in PublicApiService
- [ ] T029 [US1] Update app.routes.ts to add root route (`/`) loading LandingPageComponent
- [ ] T030 [US1] Update tenant.guard.ts to bypass tenant resolution for root (`/`) and `/public` routes
- [ ] T031 [US1] Implement hero-section component with platform branding, tagline, and value proposition
- [ ] T032 [US1] Implement organization-showcase component to display organizations from API
- [ ] T033 [US1] Add "No organizations available" message in organization-showcase when list is empty
- [ ] T034 [US1] Implement features-section component with hardcoded platform features (courses, exams, progress tracking, analytics)
- [ ] T035 [US1] Implement how-it-works-section component with 3-4 step workflow
- [ ] T036 [US1] Implement landing-page component to compose all sub-components
- [ ] T037 [US1] Add mobile-first responsive CSS for all landing page components (320px-2560px breakpoints)
- [ ] T038 [US1] Add skeleton loaders for organization showcase progressive rendering

**Checkpoint**: At this point, User Story 1 should be fully functional - landing page loads without errors and displays organizations

---

## Phase 4: User Story 2 - Organization Access (Priority: P1) 🎯 MVP

**Goal**: Visitors can click on an organization to navigate to that organization's tenant-specific area

**Independent Test**: Click on an organization card on the landing page and verify redirection to correct tenant subdomain (e.g., `https://alazhar.maken.app`)

### Implementation for User Story 2

- [ ] T039 [US2] Add click handler to organization cards in organization-showcase component
- [ ] T040 [US2] Implement navigation logic to redirect to tenant subdomain using window.location.href
- [ ] T041 [US2] Add hover effects to organization cards for visual feedback
- [ ] T042 [US2] Add touch interaction support for mobile devices
- [ ] T043 [US2] Test redirection with multiple organizations to verify correct subdomain resolution

**Checkpoint**: At this point, User Stories 1 AND 2 should both work - landing page displays organizations and clicking navigates to tenant areas

---

## Phase 5: User Story 3 - Contact for New Organization (Priority: P2)

**Goal**: Organization representatives can submit contact inquiries through a form on the landing page

**Independent Test**: Fill and submit the contact form, verify success message and database storage

### Backend Implementation for User Story 3

- [ ] T044 [P] [US3] Create CreateContactInquiryCommand in `src/Maken.Application/Commands/ContactInquiries/CreateContactInquiryCommand.cs`
- [ ] T045 [P] [US3] Create CreateContactInquiryCommandValidator in `src/Maken.Application/Commands/ContactInquiries/CreateContactInquiryCommandValidator.cs` (FluentValidation rules)
- [ ] T046 [US3] Create CreateContactInquiryCommandHandler in `src/Maken.Application/Commands/ContactInquiries/CreateContactInquiryCommandHandler.cs`
- [ ] T047 [P] [US3] Create GetContactInquiriesQuery in `src/Maken.Application/Queries/ContactInquiries/GetContactInquiriesQuery.cs` with pagination and status filter
- [ ] T048 [US3] Create GetContactInquiriesQueryHandler in `src/Maken.Application/Queries/ContactInquiries/GetContactInquiriesQueryHandler.cs`
- [ ] T049 [P] [US3] Create UpdateContactInquiryStatusCommand in `src/Maken.Application/Commands/ContactInquiries/UpdateContactInquiryStatusCommand.cs`
- [ ] T050 [US3] Create UpdateContactInquiryStatusCommandHandler in `src/Maken.Application/Commands/ContactInquiries/UpdateContactInquiryStatusCommandHandler.cs`
- [ ] T051 [US3] Create ContactInquiriesController in `src/Maken.Api/Controllers/ContactInquiriesController.cs`
- [ ] T052 [US3] Implement POST /api/contact-inquiries endpoint with [AllowAnonymous] and rate limiting (5 requests per hour)
- [ ] T053 [US3] Implement GET /api/contact-inquiries endpoint with [Authorize(Roles = "PlatformAdmin")] and pagination
- [ ] T054 [US3] Implement PATCH /api/contact-inquiries/{id}/status endpoint with [Authorize(Roles = "PlatformAdmin")]
- [ ] T055 [US3] Add AutoMapper profile for ContactInquiry → ContactInquiryDto mapping in `src/Maken.Application/Mappings/ContactInquiryMappingProfile.cs`

### Frontend Implementation for User Story 3 (Public Form)

- [ ] T056 [P] [US3] Generate contact-form component in `frontend/src/app/public/components/contact-form/`
- [ ] T057 [US3] Implement contact form with fields: contactName, email, organizationName, message
- [ ] T058 [US3] Add form validation (required fields, email format, min/max lengths)
- [ ] T059 [US3] Implement submitContactInquiry() method in PublicApiService
- [ ] T060 [US3] Add form submission handler with success/error messages
- [ ] T061 [US3] Add loading state during form submission
- [ ] T062 [US3] Add rate limit error handling (429 response)
- [ ] T063 [US3] Add mobile-responsive styling for contact form

### Frontend Implementation for User Story 3 (Admin Dashboard)

- [ ] T064 [P] [US3] Generate contact-inquiry-list component in `frontend/src/app/control-panel/contact-inquiries/`
- [ ] T065 [P] [US3] Generate contact-inquiry-detail component in `frontend/src/app/control-panel/contact-inquiries/`
- [ ] T066 [P] [US3] Create ContactInquiryService in `frontend/src/app/control-panel/contact-inquiries/services/contact-inquiry.service.ts`
- [ ] T067 [US3] Implement getContactInquiries() method in ContactInquiryService with pagination and filtering
- [ ] T068 [US3] Implement updateInquiryStatus() method in ContactInquiryService
- [ ] T069 [US3] Implement contact-inquiry-list component with table/list view, status filter, and pagination
- [ ] T070 [US3] Implement contact-inquiry-detail component with status update and notes
- [ ] T071 [US3] Add route for contact inquiries in control-panel routes: `/admin/contact-inquiries`
- [ ] T072 [US3] Add navigation link to contact inquiries in control panel sidebar
- [ ] T073 [US3] Add PlatformAdmin role guard to contact inquiry routes

**Checkpoint**: All contact form functionality complete - public form submission and admin dashboard management

---

## Phase 6: User Story 4 - Platform Information (Priority: P3)

**Goal**: Visitors can learn about platform features, benefits, and workflow through informative sections

**Independent Test**: Scroll through landing page and verify all information sections are present and readable

### Implementation for User Story 4

- [ ] T074 [US4] Enhance features-section component with detailed feature descriptions and icons
- [ ] T075 [US4] Add benefits section to landing page highlighting value for learners and organizations
- [ ] T076 [US4] Enhance how-it-works-section with clear 3-4 step process and supporting visuals
- [ ] T077 [US4] Add smooth scroll behavior for navigation between sections
- [ ] T078 [US4] Optimize content readability with proper typography and spacing
- [ ] T079 [US4] Add animations/transitions for section visibility on scroll

**Checkpoint**: All user stories complete - landing page is fully functional with all information sections

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [ ] T080 [P] Add database indexes on ContactInquiry (Status, SubmittedAt, IsDeleted) in migration
- [ ] T081 [P] Add composite index on ContactInquiry (IsDeleted, Status, SubmittedAt) for admin queries
- [ ] T082 [P] Implement rate limiting middleware for contact form endpoint (5 requests per hour per IP)
- [ ] T083 [P] Add input sanitization for ContactInquiry fields (HTML encoding)
- [ ] T084 [P] Optimize organization logos (WebP format, lazy loading)
- [ ] T085 [P] Add error logging for contact form submissions
- [ ] T086 [P] Add analytics tracking for landing page visits (if analytics service exists)
- [ ] T087 [P] Update README.md with landing page feature documentation
- [ ] T088 [P] Add API documentation comments to PublicController and ContactInquiriesController
- [ ] T089 Verify all endpoints match contracts/openapi.yaml specification
- [ ] T090 Run manual testing checklist from quickstart.md
- [ ] T091 Test responsive design on mobile (320px), tablet (768px), desktop (1024px+)
- [ ] T092 Verify landing page load time <2 seconds on standard broadband
- [ ] T093 Test with zero organizations in database (verify "no organizations" message)
- [ ] T094 Test contact form validation and error handling
- [ ] T095 Test admin dashboard filtering and pagination
- [ ] T096 Code cleanup and refactoring
- [ ] T097 Commit all changes and create pull request

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3-6)**: All depend on Foundational phase completion
  - User Story 1 (P1): Can start after Foundational - No dependencies on other stories
  - User Story 2 (P1): Depends on User Story 1 (needs organization cards to click)
  - User Story 3 (P2): Can start after Foundational - Independent of US1/US2
  - User Story 4 (P3): Can start after Foundational - Independent of other stories
- **Polish (Phase 7)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - No dependencies on other stories
- **User Story 2 (P1)**: Depends on User Story 1 completion (needs organization showcase component)
- **User Story 3 (P2)**: Can start after Foundational (Phase 2) - Independent of US1/US2
- **User Story 4 (P3)**: Can start after Foundational (Phase 2) - Independent of other stories

### Within Each User Story

- Backend tasks before frontend tasks (API must exist before frontend can consume it)
- DTOs and queries before controllers
- Services before components
- Core components before integration
- Story complete before moving to next priority

### Parallel Opportunities

- All Setup tasks marked [P] can run in parallel
- All Foundational tasks marked [P] can run in parallel (within Phase 2)
- Once Foundational phase completes:
  - User Story 1 backend tasks marked [P] can run in parallel
  - User Story 1 frontend component generation tasks marked [P] can run in parallel
  - User Story 3 backend tasks marked [P] can run in parallel
  - User Story 3 frontend tasks marked [P] can run in parallel
- User Story 3 can be developed in parallel with User Story 1 and 2 (different components)
- User Story 4 can be developed in parallel with other stories (content enhancements)
- All Polish phase tasks marked [P] can run in parallel

---

## Parallel Example: User Story 1 Backend

```bash
# Launch all DTO creation tasks together:
Task T016: "Create GetPublicOrganizationsQuery"
Task T009: "Create PublicOrganizationDto"
Task T010: "Create ContactInquiryDto"

# Launch all frontend component generation together:
Task T022: "Generate landing-page component"
Task T023: "Generate hero-section component"
Task T024: "Generate organization-showcase component"
Task T025: "Generate features-section component"
Task T026: "Generate how-it-works-section component"
```

---

## Parallel Example: User Story 3

```bash
# Launch all backend command/query creation together:
Task T044: "Create CreateContactInquiryCommand"
Task T045: "Create CreateContactInquiryCommandValidator"
Task T047: "Create GetContactInquiriesQuery"
Task T049: "Create UpdateContactInquiryStatusCommand"

# Launch all frontend component generation together:
Task T056: "Generate contact-form component"
Task T064: "Generate contact-inquiry-list component"
Task T065: "Generate contact-inquiry-detail component"
Task T066: "Create ContactInquiryService"
```

---

## Implementation Strategy

### MVP First (User Stories 1 & 2 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL - blocks all stories)
3. Complete Phase 3: User Story 1 (Platform Discovery)
4. Complete Phase 4: User Story 2 (Organization Access)
5. **STOP and VALIDATE**: Test landing page independently
6. Deploy/demo if ready

**MVP Scope**: Landing page that displays organizations and allows navigation to tenant areas - solves the "tenant not resolved" error

### Incremental Delivery

1. Complete Setup + Foundational → Foundation ready
2. Add User Story 1 + 2 → Test independently → Deploy/Demo (MVP!)
3. Add User Story 3 → Test independently → Deploy/Demo (Contact form added)
4. Add User Story 4 → Test independently → Deploy/Demo (Full information)
5. Each story adds value without breaking previous stories

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together
2. Once Foundational is done:
   - Developer A: User Story 1 (backend) + User Story 2
   - Developer B: User Story 1 (frontend components)
   - Developer C: User Story 3 (backend + public form)
   - Developer D: User Story 3 (admin dashboard) + User Story 4
3. Stories complete and integrate independently

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Backend API endpoints must be implemented before frontend components that consume them
- Mobile-first responsive design is mandatory (320px-2560px)
- Landing page must load without authentication or tenant context
- Contact form rate limiting is critical to prevent spam
- Admin dashboard requires PlatformAdmin role

---

**Total Tasks**: 97
**MVP Tasks** (US1 + US2): T001-T043 (43 tasks)
**Full Feature Tasks**: T001-T097 (97 tasks)

**Estimated Effort**:
- MVP (US1 + US2): 3-5 days
- Full Feature (US1-US4): 7-10 days
- With parallel team: 4-6 days for full feature
