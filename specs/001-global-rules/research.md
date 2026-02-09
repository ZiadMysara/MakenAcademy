# Research: Global Rules Infrastructure

**Feature**: 001-global-rules  
**Date**: 2026-02-09  
**Status**: Complete

---

## Research Topics

### R1: Multi-Tenant Database Strategy

**Question**: What's the best multi-tenant pattern for Maken's requirements?

**Research Summary**:
- **Database-per-tenant**: Maximum isolation, but high operational overhead (managing 50+ databases)
- **Schema-per-tenant**: Good isolation, but EF Core has limited support for dynamic schema switching
- **Shared database with TenantId**: Simplest, good for MVP, requires careful implementation of query filters

**Decision**: Shared database with TenantId column

**Rationale**: 
- Constitution Assumption #1 specifies single database
- EF Core 8 global query filters provide automatic tenant scoping
- Simpler operations for small team
- Can migrate to database-per-tenant later if scale demands

**Implementation Notes**:
- Add `TenantId` (Guid) to all tenant-scoped entities
- Configure global filters in `DbContext.OnModelCreating()`
- Use `IQueryable` extension to explicitly apply filter when needed

---

### R2: EF Core Global Query Filters

**Question**: How to implement automatic tenant scoping in EF Core?

**Research Summary**:
EF Core supports `HasQueryFilter()` in model configuration. Combined with a scoped `ITenantContext` service, queries are automatically filtered.

**Pattern**:
```csharp
// In DbContext
protected override void OnModelCreating(ModelBuilder builder)
{
    builder.Entity<Course>().HasQueryFilter(c => c.TenantId == _tenantContext.TenantId);
}
```

**Key Findings**:
- Filter applies to all queries, including `Include()` navigation properties
- Use `IgnoreQueryFilters()` for admin operations that need all data
- Filter doesn't apply to `FromSqlRaw()` - avoid raw SQL or manually add WHERE clause
- `DbContext` must be scoped (per-request) to pickup correct tenant

**Decision**: Use global query filters with scoped DbContext and ITenantContext

---

### R3: Subdomain-Based Tenant Resolution

**Question**: How to reliably extract tenant from subdomain?

**Research Summary**:
- Parse `Host` header in middleware
- Handle cases: `tenant.maken.app`, `www.maken.app` (redirect), `maken.app` (platform admin)
- Cache tenant lookup to avoid DB hit per request

**Pattern**:
```csharp
public class TenantMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        var host = context.Request.Host.Host; // "academy.maken.app"
        var subdomain = ExtractSubdomain(host); // "academy" or null
        
        if (subdomain != null)
        {
            var tenant = await _tenantResolver.ResolveAsync(subdomain);
            if (tenant == null) { context.Response.StatusCode = 404; return; }
            context.Items["Tenant"] = tenant;
        }
        
        await _next(context);
    }
}
```

**Caching Strategy**:
- Cache tenant by subdomain for 5 minutes (MemoryCache)
- Invalidate on tenant update
- Returns cached result within 50ms target

**Decision**: Middleware-based resolution with memory cache

---

### R4: Soft Delete Best Practices

**Question**: What's the standard pattern for soft delete in EF Core?

**Research Summary**:
- Common pattern: `IsDeleted` boolean + `DeletedAt` DateTime + optional `DeletedBy`
- Global filter: `HasQueryFilter(e => !e.IsDeleted)`
- Override `SaveChangesAsync` to intercept DELETE and convert to UPDATE

**Pattern**:
```csharp
public abstract class BaseEntity
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }
    
    public void SoftDelete(Guid deletedBy)
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
    }
}
```

**Key Findings**:
- Never expose `Delete()` method on repositories - only `SoftDelete()`
- For cascading soft delete (e.g., delete Tenant → delete all Courses), handle in Application layer
- Consider: deleted record might need to "reappear" if parent is restored

**Decision**: Base entity with soft delete fields, global filter, no physical DELETE operations

---

### R5: UUID vs Sequential ID Performance

**Question**: Does using UUID as primary key impact performance?

**Research Summary**:
- Random GUIDs can cause index fragmentation and page splits
- SQL Server supports `NEWSEQUENTIALID()` for ordered GUIDs, but not available in EF Core
- Alternative: Use sequential GUID generators (e.g., `UlidId`, RT.Comb)

**Options**:
1. Random GUID (`Guid.NewGuid()`) - simple, slight fragmentation
2. Sequential GUID (RT.Comb or similar) - ordered, better insert performance
3. ULID - sortable, but requires library

**Decision**: Use `Guid.NewGuid()` for MVP simplicity

**Rationale**:
- Constitution requires UUID, doesn't specify sequential
- MVP scale (10k users/tenant) won't show significant fragmentation impact
- Can optimize with sequential GUIDs if performance issues arise

---

### R6: Onion Architecture Project Organization

**Question**: How to structure .NET solution for Onion Architecture?

**Research Summary**:
Following Clean Architecture / Onion Architecture patterns:

```
Solution
├── src/
│   ├── Domain (no dependencies)
│   ├── Application (depends on Domain)
│   ├── Infrastructure (depends on Application, implements interfaces)
│   └── Api (depends on Application, references Infrastructure for DI)
└── tests/
    ├── Domain.Tests
    ├── Application.Tests
    ├── Infrastructure.Tests
    └── Api.Tests
```

**Key Principles**:
- Domain has ZERO external dependencies (no NuGet packages except pure abstractions)
- Application defines interfaces, Infrastructure implements them
- Api only references Infrastructure for service registration
- Tests mirror source structure

**Decision**: 4 projects matching Constitution architecture requirement

---

### R7: Role-Based Authorization in ASP.NET Core

**Question**: How to implement the 4-role system defined in Constitution?

**Research Summary**:
- ASP.NET Core supports policy-based and role-based authorization
- Claims-based approach: Store role as claim, use `[Authorize(Roles = "CompanyAdmin")]`
- For tenant-scoped roles: Combine role check with tenant context

**Pattern**:
```csharp
// In controller
[Authorize(Roles = "CompanyAdmin,PlatformAdmin")]
public async Task<IActionResult> ManageCourses() { ... }

// Custom policy for tenant + role
services.AddAuthorization(options =>
{
    options.AddPolicy("TenantAdmin", policy =>
        policy.RequireRole("CompanyAdmin")
              .AddRequirements(new TenantMemberRequirement()));
});
```

**Key Findings**:
- PlatformAdmin has no TenantId - must handle specially
- Instructor and Student are tenant-scoped
- Store role as enum in User entity, map to claims on login

**Decision**: Enum-based roles with claims mapping for authorization

---

## Summary of Decisions

| Topic | Decision | Constitution Alignment |
|-------|----------|----------------------|
| Multi-tenant pattern | Shared DB + TenantId | ✅ Matches Assumption #1 |
| Query scoping | EF Core global filters | ✅ Enforces FR-002 |
| Tenant resolution | Subdomain middleware + cache | ✅ Matches §4 |
| Soft delete | Base entity pattern | ✅ Matches §6 |
| Primary keys | `Guid.NewGuid()` | ✅ Matches FR-006, FR-007 |
| Architecture | 4-project Onion | ✅ Matches §3 |
| Authorization | Enum roles + claims | ✅ Matches §4 Role System |

---

**Research Status**: Complete  
**Proceed to**: Phase 1 - Design & Contracts
