# Maken

**Multi-tenant educational platform for Islamic Science Institutes**

Maken is a product-first, methodology-driven learning management system designed specifically for Islamic Science Institutes. It provides structured progression rules, course prerequisites, and level sequencing to ensure students follow a guided learning path.

---

## 🎯 Project Overview

**Status**: Phase 6 Complete (62/70 tasks, 89%)  
**Build**: ✅ SUCCESS  
**Tests**: ✅ 76/76 PASSING

### Key Features

- ✅ **Multi-tenant Architecture**: Complete data isolation per institute
- ✅ **Subdomain-based Tenant Resolution**: Each institute gets their own subdomain (e.g., `academy.maken.app`)
- ✅ **Global Rules Enforcement**: Automatic soft delete and tenant scoping
- ✅ **Role-based Access Control**: PlatformAdmin, CompanyAdmin, Instructor, Student
- ✅ **Progression Rule Infrastructure**: Foundation for course/lesson prerequisites
- ✅ **Health Check Endpoint**: Production-ready monitoring
- ✅ **API Response Standards**: Consistent error handling and response formats

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

- **Total Tests**: 76 (all passing)
- **Domain Tests**: 28 tests
- **Infrastructure Tests**: 24 tests
- **API Tests**: 24 tests

### Test Categories

- Unit tests for domain entities and business rules
- Integration tests for database operations and global filters
- API integration tests for middleware and endpoints

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

## 📊 API Endpoints

### Health Check

```
GET /api/v1/health
```

Returns system health status including database connectivity.

**Response**:
```json
{
  "status": "healthy",
  "version": "1.0.0",
  "timestamp": "2026-02-09T14:30:00Z",
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

## 📖 Documentation

- **Constitution**: `.specify/memory/constitution.md` - Non-negotiable project rules
- **Specifications**: `specs/001-global-rules/` - Feature specifications and tasks
- **Phase Reports**: `docs/phase-reports/` - Completion reports for each phase
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

### 🔄 Current Phase

- **Phase 7**: Polish & cross-cutting concerns (in progress)
  - Global exception handling ✅
  - Swagger/OpenAPI documentation ✅
  - Seed data script ✅
  - README.md ✅
  - Code reviews (pending)

### 📅 Future Phases

- Course and lesson management
- Exam system with MCQ support
- Progress tracking
- Analytics dashboard
- Frontend (Angular 20)

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
