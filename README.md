# Maken

**Multi-tenant educational platform for Islamic Science Institutes**

Maken is a product-first, methodology-driven learning management system designed specifically for Islamic Science Institutes. It provides structured progression rules, course prerequisites, and level sequencing to ensure students follow a guided learning path.

---

## 🎯 Project Overview

**Status**: Phase 13 Complete (226/228 tasks, 99%)  
**Build**: ✅ SUCCESS  
**Tests**: ✅ ALL PASSING

### Key Features

- ✅ **Multi-tenant Architecture**: Complete data isolation per institute
- ✅ **Subdomain-based Tenant Resolution**: Each institute gets their own subdomain (e.g., `academy.maken.app`)
- ✅ **Public Landing Page**: Anonymous access to platform information and organization showcase
- ✅ **Contact Form**: Organizations can submit inquiries to join the platform
- ✅ **Course Management**: Full CRUD operations for courses with draft/published status
- ✅ **Lesson Management**: Create and organize lessons with video/PDF content
- ✅ **Exam System**: MCQ exams with automatic grading and unlimited retries
- ✅ **Student Enrollment**: Enroll students in courses with progress tracking
- ✅ **Progression Rules**: Sequential lesson unlocking and course prerequisites
- ✅ **Analytics Dashboard**: Enrollment counts, completion rates, and exam pass rates
- ✅ **JWT Authentication**: Secure token-based authentication with refresh tokens
- ✅ **Role-based Access Control**: PlatformAdmin, CompanyAdmin, Instructor, Student
- ✅ **Soft Delete**: Cascade soft delete for all entities
- ✅ **Health Check Endpoint**: Production-ready monitoring

---

## 🏗️ Architecture

### Tech Stack

- **Backend**: ASP.NET Core 8 Web API
- **Database**: PostgreSQL (via Supabase)
- **ORM**: Entity Framework Core 8
- **Authentication**: JWT Bearer (Supabase Auth)
- **Frontend**: Angular 20 (planned)
- **Infrastructure**: Supabase (PostgreSQL, Auth, Storage, Vault)

### Onion Architecture

```
Domain → Application → Infrastructure → API (Presentation)
```

- **Domain Layer**: Pure business entities and rules (no dependencies)
- **Application Layer**: Use cases, validation, MediatR handlers
- **Infrastructure Layer**: Database access, external services, caching
- **API Layer**: REST endpoints, middleware, request/response mapping

---

## 🚀 Getting Started

### Prerequisites

- .NET 8 SDK
- PostgreSQL (or Supabase account)
- Visual Studio 2022 / VS Code / Rider

### Quick Start

1. **Clone the repository**
   ```bash
   git clone https://github.com/your-org/maken.git
   cd maken
   ```

2. **Configure database connection**
   
   Update `src/Maken.Api/appsettings.Development.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Database=maken;Username=postgres;Password=yourpassword"
     }
   }
   ```

3. **Apply migrations**
   ```bash
   dotnet ef database update --project src/Maken.Infrastructure --startup-project src/Maken.Api
   ```

4. **Run the API**
   ```bash
   dotnet run --project src/Maken.Api
   ```

5. **Access Swagger UI**
   
   Navigate to: `https://localhost:5001/swagger`

### Seed Demo Data

Run the seed script to create demo tenant and users:

```powershell
.\scripts\seed-database.ps1
```

**Demo Credentials**:
- Subdomain: `demo.maken.app`
- Platform Admin: `admin@maken.app`
- Company Admin: `admin@demo.maken.app`
- Instructor: `instructor@demo.maken.app`
- Student: `student@demo.maken.app`

*Note: Passwords must be set via Supabase Auth*

---

## 🧪 Testing

### Run All Tests

```bash
dotnet test
```

### Test Coverage

- **Total Tests**: All passing
- **Domain Tests**: Entity business logic and domain services
- **Application Tests**: Command/query handlers and property-based tests
- **Infrastructure Tests**: Repository operations and database integration
- **API Tests**: Controller endpoints and integration flows

### Test Categories

- Unit tests for domain entities and business rules
- Property-based tests for correctness properties (29 properties with 100+ iterations each)
- Integration tests for database operations and global filters
- API integration tests for middleware and endpoints
- End-to-end tests for complete user workflows

---

## 📁 Project Structure

```
maken/
├── src/
│   ├── Maken.Domain/           # Core business entities and rules
│   ├── Maken.Application/      # Use cases and application logic
│   ├── Maken.Infrastructure/   # Database, external services
│   └── Maken.Api/              # REST API endpoints
├── tests/
│   ├── Maken.Domain.Tests/
│   ├── Maken.Application.Tests/
│   ├── Maken.Infrastructure.Tests/
│   └── Maken.Api.Tests/
├── specs/                      # Feature specifications
├── docs/                       # Documentation and phase reports
└── scripts/                    # Utility scripts
```

---

## 🔑 Core Concepts

### Tenant Isolation

Every entity (except PlatformAdmin users) belongs to exactly one tenant. Tenant resolution happens automatically via subdomain:

- Request to `academy.maken.app` → Tenant "academy"
- Request to `demo.maken.app` → Tenant "demo"
- Unknown subdomain → 404 Not Found

### Soft Delete

All entities support soft delete. Physical deletion is prohibited:

```csharp
entity.SoftDelete(); // Sets IsDeleted = true, DeletedAt = UTC now
```

Global query filters automatically exclude soft-deleted records.

### UUID Primary Keys

All entities use server-generated UUIDs (Guid) as primary keys:

```csharp
public class BaseEntity
{
    public Guid Id { get; private set; } = Guid.NewGuid();
}
```

### Role System

| Role | Scope | Permissions |
|------|-------|-------------|
| PlatformAdmin | Global | Full system access |
| CompanyAdmin | Tenant | Manage tenant configuration |
| Instructor | Tenant | Create courses and lessons |
| Student | Tenant | Access courses and take exams |

---

## 🛠️ Development

### Build

```bash
dotnet build
```

### Run API

```bash
dotnet run --project src/Maken.Api
```

### Create Migration

```bash
dotnet ef migrations add MigrationName --project src/Maken.Infrastructure --startup-project src/Maken.Api
```

### Apply Migration

```bash
dotnet ef database update --project src/Maken.Infrastructure --startup-project src/Maken.Api
```

---

## 📚 Features

### Course Management
Create, update, and manage courses with draft/published workflow. Courses support:
- Free-flow mode (students can access lessons in any order)
- Sequential mode (lessons unlock one by one)
- Course prerequisites (require completion of other courses)
- Tenant isolation (courses are scoped to specific institutes)

### Lesson Management
Organize course content into structured lessons:
- Video and PDF content types
- Ordered lesson sequences
- Automatic progression tracking
- Soft delete with cascade to exams and progress

### Exam System
Multiple-choice question (MCQ) exams with:
- Automatic grading based on correct answers
- Configurable pass thresholds (0-100%)
- Unlimited exam attempts
- Exam results stored in student progress
- Answer visibility control (hidden for students, visible for admins)

### Student Enrollment
Manage student access to courses:
- Enroll students in specific courses
- Track enrollment dates and completion status
- Prevent duplicate enrollments
- View all enrollments per course

### Progress Tracking
Monitor student learning progress:
- Lesson completion tracking
- Exam results and scores
- Course completion percentage
- Sequential lesson unlocking based on completion

### Analytics Dashboard
View tenant-scoped analytics:
- Total enrollment count
- Course completion rate
- Exam pass rate
- All metrics isolated per tenant

### Public Landing Page
Anonymous access to platform information:
- Platform branding and value proposition
- Organization showcase (active organizations only)
- Platform features overview
- How it works section
- Contact form for new organization inquiries
- Mobile-first responsive design (320px-2560px)

### Contact Inquiry Management
Admin dashboard for managing contact inquiries:
- View all contact inquiries with pagination
- Filter by status (New, Reviewed, Contacted)
- Update inquiry status and add notes
- PlatformAdmin role required

### Authentication & Authorization
Secure access control:
- JWT-based authentication
- Refresh token support
- Role-based authorization (PlatformAdmin, CompanyAdmin, Instructor, Student)
- Tenant resolution from subdomain

---

## 📊 API Endpoints

### Base URL
- Development: `https://localhost:7001/api`
- Production: `https://api.maken.app/api`

### Authentication
All endpoints (except `/auth/*` and `/tenants/resolve`) require JWT authentication:
```
Authorization: Bearer <access_token>
```

### Health Check

#### `GET /api/v1/health`
Returns system health status including database connectivity.

**Authorization**: None (public endpoint)

**Response**:
```json
{
  "status": "healthy",
  "version": "1.0.0",
  "timestamp": "2026-02-10T14:30:00Z",
  "checks": [
    {
      "name": "database",
      "status": "healthy",
      "duration": "12ms"
    }
  ]
}
```

---

### Public Landing Page

#### `GET /`
Public landing page accessible without authentication.

**Authorization**: None (public endpoint)

**Features**:
- Platform branding and hero section
- Organization showcase (active organizations only)
- Platform features overview
- How it works section
- Contact form for new organization inquiries

#### `GET /api/public/organizations`
Gets a list of active organizations for the landing page.

**Authorization**: None (public endpoint)

**Response**:
```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "Demo Academy",
    "subdomain": "demo",
    "logoUrl": null,
    "primaryColor": null,
    "secondaryColor": null
  }
]
```

#### `POST /api/contact-inquiries`
Submits a contact inquiry from the public landing page.

**Authorization**: None (public endpoint)

**Request Body**:
```json
{
  "contactName": "John Doe",
  "email": "john@example.com",
  "organizationName": "Example Institute",
  "message": "We are interested in using Maken for our institute..."
}
```

**Response**:
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "message": "Your inquiry has been submitted successfully. We will contact you soon."
}
```

#### `GET /api/contact-inquiries`
Gets all contact inquiries with pagination (admin only).

**Authorization**: Required (PlatformAdmin only)

**Query Parameters**:
- `pageNumber` (optional, default: 1): Page number
- `pageSize` (optional, default: 20): Items per page
- `status` (optional): Filter by status (New, Reviewed, Contacted)

**Response**:
```json
{
  "items": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "contactName": "John Doe",
      "email": "john@example.com",
      "organizationName": "Example Institute",
      "message": "We are interested in using Maken...",
      "status": "New",
      "submittedAt": "2026-02-11T20:00:00Z",
      "reviewedAt": null,
      "reviewedBy": null,
      "notes": null
    }
  ],
  "totalCount": 1,
  "pageNumber": 1,
  "pageSize": 20,
  "totalPages": 1
}
```

#### `PATCH /api/contact-inquiries/{id}/status`
Updates the status of a contact inquiry (admin only).

**Authorization**: Required (PlatformAdmin only)

**Request Body**:
```json
{
  "status": "Reviewed",
  "notes": "Contacted via email on 2026-02-12"
}
```

**Response**: 204 No Content

---

### Authentication

#### `POST /api/auth/login`
Authenticates a user and returns JWT tokens.

**Authorization**: None (public endpoint)

**Request Body**:
```json
{
  "email": "user@example.com",
  "password": "password123"
}
```

**Response**:
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "refresh_token_here",
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "email": "user@example.com",
  "role": "Student"
}
```

#### `POST /api/auth/refresh`
Refreshes an access token using a valid refresh token.

**Authorization**: None (public endpoint)

**Request Body**:
```json
{
  "refreshToken": "refresh_token_here"
}
```

**Response**:
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "new_refresh_token_here"
}
```

---

### Tenant Management

#### `GET /api/tenants/resolve?subdomain={subdomain}`
Resolves a tenant by subdomain.

**Authorization**: None (public endpoint)

**Query Parameters**:
- `subdomain` (required): The tenant subdomain (e.g., "academy")

**Response**:
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "Academy Institute",
  "subdomain": "academy",
  "isActive": true
}
```

---

### Course Management

#### `GET /api/courses`
Gets a paginated list of courses for the current tenant.

**Authorization**: Required (any authenticated user)

**Query Parameters**:
- `pageNumber` (optional, default: 1): Page number
- `pageSize` (optional, default: 20): Items per page

**Response**:
```json
{
  "courses": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "name": "Introduction to Islamic Studies",
      "description": "A comprehensive introduction...",
      "status": "Published",
      "freeFlowMode": false,
      "prerequisiteCourseIds": [],
      "isLocked": false,
      "completionPercentage": 0,
      "createdAt": "2026-02-10T10:00:00Z",
      "updatedAt": null
    }
  ],
  "totalCount": 1,
  "pageNumber": 1,
  "pageSize": 20
}
```

#### `GET /api/courses/{id}`
Gets a specific course by ID.

**Authorization**: Required (any authenticated user)

**Response**: Single course object (same structure as list item above)

#### `POST /api/courses`
Creates a new course.

**Authorization**: Required (CompanyAdmin or Instructor only)

**Request Body**:
```json
{
  "name": "Introduction to Islamic Studies",
  "description": "A comprehensive introduction to Islamic studies",
  "freeFlowMode": false,
  "prerequisiteCourseIds": []
}
```

**Response**: Created course object with 201 status

#### `PUT /api/courses/{id}`
Updates an existing course.

**Authorization**: Required (CompanyAdmin or Instructor only)

**Request Body**: Same as POST

**Response**: Updated course object

#### `DELETE /api/courses/{id}`
Soft deletes a course and cascades to related entities (lessons, exams, enrollments).

**Authorization**: Required (CompanyAdmin or Instructor only)

**Response**: 204 No Content

#### `POST /api/courses/{id}/publish`
Publishes a course, making it visible to students.

**Authorization**: Required (CompanyAdmin or Instructor only)

**Response**: Published course object

---

### Lesson Management

#### `GET /api/courses/{courseId}/lessons`
Gets all lessons for a course, ordered by Order field.

**Authorization**: Required (any authenticated user)

**Response**:
```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "courseId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "title": "Lesson 1: Foundations",
    "description": "Introduction to the foundations",
    "contentType": "Video",
    "contentUrl": "https://youtube.com/watch?v=example",
    "order": 1,
    "isLocked": false,
    "isCompleted": false,
    "createdAt": "2026-02-10T10:00:00Z",
    "updatedAt": null
  }
]
```

#### `GET /api/courses/{courseId}/lessons/{lessonId}`
Gets a specific lesson by ID.

**Authorization**: Required (any authenticated user)

**Response**: Single lesson object (same structure as list item above)

#### `POST /api/courses/{courseId}/lessons`
Creates a new lesson in a course.

**Authorization**: Required (CompanyAdmin or Instructor only)

**Request Body**:
```json
{
  "title": "Lesson 1: Foundations",
  "description": "Introduction to the foundations",
  "contentType": "Video",
  "contentUrl": "https://youtube.com/watch?v=example",
  "order": 1
}
```

**Response**: Created lesson object with 201 status

#### `PUT /api/courses/{courseId}/lessons/{lessonId}`
Updates an existing lesson.

**Authorization**: Required (CompanyAdmin or Instructor only)

**Request Body**: Same as POST

**Response**: Updated lesson object

#### `DELETE /api/courses/{courseId}/lessons/{lessonId}`
Soft deletes a lesson and cascades to related entities (exam, progress).

**Authorization**: Required (CompanyAdmin or Instructor only)

**Response**: 204 No Content

---

### Exam Management

#### `GET /api/exams/{id}`
Gets an exam by ID.

**Authorization**: Required (any authenticated user)

**Query Parameters**:
- `includeAnswers` (optional, default: false): Include correct answers (admin/instructor only)

**Response**:
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "lessonId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "tenantId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "title": "Lesson 1 Quiz",
  "passThreshold": 70,
  "questions": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "examId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "text": "What is the first pillar of Islam?",
      "order": 1,
      "choices": [
        {
          "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
          "questionId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
          "text": "Shahada",
          "isCorrect": null
        }
      ]
    }
  ],
  "createdAt": "2026-02-10T10:00:00Z",
  "updatedAt": null
}
```

#### `POST /api/exams`
Creates a new exam with questions and choices.

**Authorization**: Required (CompanyAdmin or Instructor only)

**Request Body**:
```json
{
  "lessonId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "title": "Lesson 1 Quiz",
  "passThreshold": 70,
  "questions": [
    {
      "text": "What is the first pillar of Islam?",
      "order": 1,
      "choices": [
        { "text": "Shahada", "isCorrect": true },
        { "text": "Salah", "isCorrect": false },
        { "text": "Zakat", "isCorrect": false },
        { "text": "Sawm", "isCorrect": false }
      ]
    }
  ]
}
```

**Response**: Created exam object with 201 status

#### `PUT /api/exams/{id}`
Updates an existing exam.

**Authorization**: Required (CompanyAdmin or Instructor only)

**Request Body**: Similar to POST but includes question/choice IDs for updates

**Response**: Updated exam object

#### `DELETE /api/exams/{id}`
Soft deletes an exam and cascades to questions and choices.

**Authorization**: Required (CompanyAdmin or Instructor only)

**Response**: 204 No Content

#### `GET /api/courses/{courseId}/lessons/{lessonId}/exam`
Gets the exam for a specific lesson (student view).

**Authorization**: Required (any authenticated user)

**Query Parameters**:
- `includeAnswers` (optional, default: false): Include correct answers (admin/instructor only)

**Response**: Exam object (same structure as GET /api/exams/{id})

#### `POST /api/courses/{courseId}/lessons/{lessonId}/exam`
Submits exam answers for a specific lesson.

**Authorization**: Required (Student only)

**Request Body**:
```json
{
  "answers": [
    {
      "questionId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "selectedChoiceId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
    }
  ]
}
```

**Response**:
```json
{
  "examId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "studentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "passed": true,
  "score": 75,
  "totalQuestions": 4,
  "correctAnswers": 3,
  "attemptedAt": "2026-02-10T14:30:00Z"
}
```

---

### Enrollment Management

#### `GET /api/courses/{courseId}/enrollments`
Gets all enrollments for a specific course.

**Authorization**: Required (CompanyAdmin or Instructor only)

**Response**:
```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "studentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "studentFirstName": "John",
    "studentLastName": "Doe",
    "studentEmail": "john.doe@example.com",
    "courseId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "enrolledAt": "2026-02-10T10:00:00Z",
    "completedAt": null
  }
]
```

#### `POST /api/courses/{courseId}/enrollments`
Enrolls a student in a course.

**Authorization**: Required (CompanyAdmin or Instructor only)

**Request Body**:
```json
{
  "studentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

**Response**: Created enrollment object with 201 status

---

### Student Operations

#### `POST /api/students/lessons/{lessonId}/complete`
Marks a lesson as completed for the current student.

**Authorization**: Required (Student only)

**Response**:
```json
{
  "message": "Lesson marked as complete."
}
```

---

### Analytics

#### `GET /api/analytics`
Gets analytics data for the current tenant.

**Authorization**: Required (CompanyAdmin only)

**Response**:
```json
{
  "totalEnrollments": 150,
  "completionRate": 68.5,
  "examPassRate": 82.3
}
```

---

## 📖 Documentation

For detailed information, see:

- **Quickstart Guide**: `specs/009-backend-business-features/quickstart.md` - Setup and development workflow
- **API Examples**: See quickstart guide for detailed request/response examples
- **Constitution**: `.specify/memory/constitution.md` - Non-negotiable project rules
- **Specifications**: `specs/009-backend-business-features/` - Feature specifications and tasks
- **Architecture Guide**: `.kiro/context/architecture.md` - Technical patterns and examples

---

## 🎯 Roadmap

### ✅ Completed Phases

- **Phase 1**: Solution setup and project structure
- **Phase 2**: Foundation (BaseEntity, core entities, EF Core)
- **Phase 3**: Global rules (soft delete, tenant scoping)
- **Phase 4**: Tenant data isolation (subdomain resolution)
- **Phase 5**: Progression rule infrastructure
- **Phase 6**: Health check and API contracts
- **Phase 7**: Authentication and tenant resolution (User Story 9)
- **Phase 8**: Course management (User Story 1)
- **Phase 9**: Lesson management (User Story 2)
- **Phase 10**: Course browsing (User Story 4)
- **Phase 11**: Lesson viewing and progress tracking (User Story 5)
- **Phase 12**: Exam management (User Story 3)
- **Phase 13**: Exam taking (User Story 6)
- **Phase 14**: Student enrollment (User Story 7)
- **Phase 15**: Analytics dashboard (User Story 8)
- **Phase 16**: Cross-cutting concerns and property tests
- **Phase 17**: Polish and final validation

### 🔄 Current Phase

- **Phase 18**: Documentation and deployment preparation (in progress)

### 📅 Future Phases

- Frontend integration (Angular 20)
- Advanced analytics and reporting
- Email notifications
- File upload and storage integration

---

## 🤝 Contributing

This project follows spec-driven development:

1. All features must have an approved specification
2. Specifications follow the format in `specs/`
3. Implementation follows the task list in `tasks.md`
4. All code must pass tests and comply with the Constitution

---

## 📄 License

Copyright © 2026 Maken. All rights reserved.

---

## 📞 Support

For questions or issues, please contact:
- Email: support@maken.app
- Documentation: See `docs/` directory

---

**Built with ❤️ for Islamic Science Institutes**
