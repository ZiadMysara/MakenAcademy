# Quickstart: Maken Platform Development

**Feature**: 001-global-rules  
**Date**: 2026-02-09

---

## Prerequisites

- **.NET 8.0 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
- **SQL Server** - Local instance or Docker container
- **IDE** - Visual Studio 2022 / VS Code with C# extension / JetBrains Rider
- **Git** - Version control

---

## 1. Clone Repository

```bash
git clone https://github.com/YourOrg/Maken.git
cd Maken
git checkout 001-global-rules
```

---

## 2. Database Setup

### Option A: SQL Server LocalDB (Windows)

LocalDB is included with Visual Studio.

```bash
# Create database
sqllocaldb create MakenDb
sqllocaldb start MakenDb
```

Connection string:
```
Server=(localdb)\MSSQLLocalDB;Database=MakenDb;Trusted_Connection=True;
```

### Option B: Docker (Cross-platform)

```bash
# Start SQL Server container
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=YourStrong@Passw0rd" \
  -p 1433:1433 --name maken-sql -d mcr.microsoft.com/mssql/server:2022-latest
```

Connection string:
```
Server=localhost,1433;Database=MakenDb;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;
```

---

## 3. Configure Application

Copy the example settings file:

```bash
cd src/Maken.Api
cp appsettings.Development.example.json appsettings.Development.json
```

Update connection string in `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "YOUR_CONNECTION_STRING_HERE"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

---

## 4. Apply Migrations

```bash
cd src/Maken.Infrastructure
dotnet ef database update --startup-project ../Maken.Api
```

Or from solution root:

```bash
dotnet ef database update --project src/Maken.Infrastructure --startup-project src/Maken.Api
```

---

## 5. Run the Application

```bash
cd src/Maken.Api
dotnet run
```

The API will be available at:
- `https://localhost:7001` (HTTPS)
- `http://localhost:5001` (HTTP)

### Health Check

```bash
curl http://localhost:5001/api/v1/health
```

Expected response:
```json
{
  "status": "healthy",
  "version": "1.0.0",
  "timestamp": "2026-02-09T04:25:00Z",
  "checks": [
    { "name": "database", "status": "healthy", "duration": "15ms" }
  ]
}
```

---

## 6. Run Tests

```bash
# All tests
dotnet test

# Specific project
dotnet test tests/Maken.Domain.Tests
dotnet test tests/Maken.Application.Tests
dotnet test tests/Maken.Infrastructure.Tests
dotnet test tests/Maken.Api.Tests
```

---

## 7. Development Workflow

### Create a New Migration

After modifying entities in Domain:

```bash
cd src/Maken.Infrastructure
dotnet ef migrations add MigrationName --startup-project ../Maken.Api
```

### Run with Hot Reload

```bash
cd src/Maken.Api
dotnet watch run
```

---

## Project Structure Reference

```
Maken/
├── src/
│   ├── Maken.Domain/           # Entities, Value Objects, Enums
│   ├── Maken.Application/      # Use-cases, Interfaces, Behaviors
│   ├── Maken.Infrastructure/   # DbContext, Repositories, Migrations
│   └── Maken.Api/              # Controllers, Middleware, Program.cs
├── tests/
│   ├── Maken.Domain.Tests/
│   ├── Maken.Application.Tests/
│   ├── Maken.Infrastructure.Tests/
│   └── Maken.Api.Tests/
├── specs/                      # Feature specifications
│   └── 001-global-rules/
│       ├── spec.md
│       ├── plan.md
│       ├── research.md
│       ├── data-model.md
│       ├── quickstart.md
│       └── contracts/
└── Maken.sln
```

---

## Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `ASPNETCORE_ENVIRONMENT` | Runtime environment | `Development` |
| `ConnectionStrings__DefaultConnection` | Database connection | (none) |

---

## Troubleshooting

### Migration fails with "No migrations to apply"

Ensure the database exists:
```bash
dotnet ef database drop --force --project src/Maken.Infrastructure --startup-project src/Maken.Api
dotnet ef database update --project src/Maken.Infrastructure --startup-project src/Maken.Api
```

### Port already in use

Change ports in `launchSettings.json`:
```json
{
  "profiles": {
    "Maken.Api": {
      "applicationUrl": "https://localhost:7002;http://localhost:5002"
    }
  }
}
```

### Subdomain testing locally

Add entries to your hosts file:

**Windows** (`C:\Windows\System32\drivers\etc\hosts`):
```
127.0.0.1 demo.maken.local
127.0.0.1 academy.maken.local
```

**macOS/Linux** (`/etc/hosts`):
```
127.0.0.1 demo.maken.local
127.0.0.1 academy.maken.local
```

Then access via `http://demo.maken.local:5001`

---

## Next Steps

1. Seed initial data (PlatformAdmin user, demo tenant)
2. Implement authentication endpoints
3. Add tenant management APIs

---

**Quickstart Version**: 1.0
