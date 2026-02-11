# Feature Specification: Public Landing Page

**Feature Branch**: `010-public-landing-page`  
**Created**: 2025-02-11  
**Status**: Draft  
**Input**: User description: "Create a public landing page that serves as the entry point for the Maken platform, showcasing available organizations and providing contact options for new brands"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Platform Discovery (Priority: P1) 🎯 MVP

A visitor arrives at the Maken platform root URL (without any subdomain) and wants to understand what the platform offers and see which organizations are using it.

**Why this priority**: This is the critical entry point for all new users. Without this, users see an error when visiting the root domain, creating a poor first impression and blocking access to the platform.

**Independent Test**: Can be fully tested by visiting the root URL (e.g., `https://maken.app`) and verifying that a landing page loads with platform information and a list of available organizations, delivering immediate value by allowing users to discover and access organizations.

**Acceptance Scenarios**:

1. **Given** a visitor navigates to the root URL without a subdomain, **When** the page loads, **Then** they see a welcoming landing page with platform branding, tagline, and value proposition
2. **Given** the landing page is displayed, **When** the visitor scrolls down, **Then** they see a section showcasing available organizations/brands with their logos and names
3. **Given** multiple organizations exist in the system, **When** the landing page loads, **Then** only active/published organizations are displayed in the showcase
4. **Given** no organizations exist in the system, **When** the landing page loads, **Then** a message encourages visitors to "Be the first organization on Maken" with a contact call-to-action

---

### User Story 2 - Organization Access (Priority: P1) 🎯 MVP

A visitor sees an organization they want to access and clicks on it to navigate to that organization's tenant-specific area.

**Why this priority**: This is the primary navigation mechanism from the public landing page to tenant-specific areas. It must work for the landing page to fulfill its purpose as an entry point.

**Independent Test**: Can be fully tested by clicking on an organization card/link on the landing page and verifying redirection to the correct tenant subdomain (e.g., `https://alazhar.maken.app`), delivering immediate value by providing seamless access to organizations.

**Acceptance Scenarios**:

1. **Given** a visitor is on the landing page, **When** they click on an organization card, **Then** they are redirected to that organization's subdomain URL
2. **Given** a visitor clicks on an organization, **When** the redirect occurs, **Then** the tenant context is properly set and they land on the organization's login or home page
3. **Given** an organization card is displayed, **When** a visitor hovers over it, **Then** visual feedback indicates it is clickable
4. **Given** a visitor is on mobile, **When** they tap an organization card, **Then** the touch interaction works smoothly without delays

---

### User Story 3 - Contact for New Organization (Priority: P2)

An organization representative wants to create their own branded learning platform and needs to contact the Maken team.

**Why this priority**: This enables business growth by providing a clear path for new organizations to onboard. While important, it's secondary to allowing access to existing organizations.

**Independent Test**: Can be fully tested by clicking the "Contact Us" or "Create Your Brand" button and verifying that a contact form or email link is presented, delivering value by providing a clear onboarding path for new organizations.

**Acceptance Scenarios**:

1. **Given** a visitor is on the landing page, **When** they click "Contact Us" or "Create Your Brand", **Then** a contact form modal appears or they are directed to a contact page
2. **Given** the contact form is displayed, **When** the visitor fills in their name, email, organization name, and message, **Then** the form validates the input
3. **Given** the contact form is completed with valid data, **When** the visitor submits it, **Then** the message is sent to the Maken admin team and the visitor sees a success confirmation
4. **Given** the contact form submission fails, **When** an error occurs, **Then** the visitor sees a clear error message and can retry or use an alternative contact method (email address displayed)

---

### User Story 4 - Platform Information (Priority: P3)

A visitor wants to learn more about the Maken platform's features, benefits, and how it works before deciding to access an organization or contact for their own brand.

**Why this priority**: This provides context and builds trust, but users can still access organizations without reading detailed information. It's valuable for conversion but not blocking.

**Independent Test**: Can be fully tested by scrolling through the landing page and verifying that feature highlights, benefits, and "How It Works" sections are present and informative, delivering value by educating potential users and organizations.

**Acceptance Scenarios**:

1. **Given** a visitor is on the landing page, **When** they scroll down, **Then** they see sections describing key platform features (courses, exams, progress tracking, analytics)
2. **Given** the features section is displayed, **When** the visitor reads it, **Then** each feature is explained in simple, non-technical language with supporting icons or images
3. **Given** a visitor wants to understand the platform workflow, **When** they view the "How It Works" section, **Then** they see a clear 3-4 step process (e.g., "1. Choose Organization → 2. Login/Signup → 3. Start Learning")
4. **Given** a visitor is interested in benefits, **When** they view the benefits section, **Then** they see value propositions for both learners and organizations

---

### Edge Cases

- What happens when a visitor tries to access a tenant subdomain that doesn't exist? (Should show a "Organization not found" page with a link back to the landing page)
- How does the system handle when all organizations are inactive/unpublished? (Show "No organizations available" message with contact option)
- What happens if the contact form submission fails due to network issues? (Show error message with retry option and display admin email as fallback)
- How does the landing page perform on slow connections? (Should load progressively with critical content first, show loading states)
- What happens when a visitor accesses the landing page while already logged into a tenant? (Show personalized message with link to their organization's dashboard)

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST display a public landing page when users visit the root domain without a subdomain
- **FR-002**: Landing page MUST showcase the platform name, logo, tagline, and value proposition prominently
- **FR-003**: Landing page MUST display a list of all active/published organizations with their names and logos
- **FR-004**: System MUST allow visitors to click on an organization to navigate to that organization's tenant-specific subdomain
- **FR-005**: Landing page MUST provide a "Contact Us" or "Create Your Brand" call-to-action button
- **FR-006**: System MUST provide a contact form or contact method for organizations interested in creating their own brand
- **FR-007**: Landing page MUST include sections describing platform features and benefits
- **FR-008**: Landing page MUST be responsive and work on desktop, tablet, and mobile devices
- **FR-009**: System MUST handle the case when no organizations exist by displaying an appropriate message
- **FR-010**: Landing page MUST load without requiring authentication or tenant context
- **FR-011**: System MUST filter out inactive or unpublished organizations from the landing page display
- **FR-012**: Landing page MUST include a "How It Works" section explaining the platform workflow in 3-4 simple steps

### Key Entities *(include if feature involves data)*

- **Organization/Tenant**: Represents a branded learning platform instance with name, subdomain, logo, active status, and branding colors
- **Contact Inquiry**: Represents a message from a potential organization with contact name, email, organization name, and message content
- **Landing Page Content**: Represents configurable content sections including hero text, features list, benefits, and how-it-works steps

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Visitors can access the landing page and view available organizations within 2 seconds on standard broadband connections
- **SC-002**: 95% of visitors successfully navigate from the landing page to a tenant-specific area on their first attempt
- **SC-003**: Contact form submissions are successfully delivered to the admin team with 99% reliability
- **SC-004**: Landing page is fully functional and visually correct on devices with screen widths from 320px (mobile) to 2560px (desktop)
- **SC-005**: Zero "tenant not resolved" errors occur when visitors access the root domain
- **SC-006**: Visitors can understand the platform's purpose and value within 10 seconds of landing on the page (measured through user testing or analytics showing engagement with key sections)
