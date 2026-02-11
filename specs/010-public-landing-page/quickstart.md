# Quickstart Guide: Public Landing Page

**Feature**: Public Landing Page  
**Branch**: `010-public-landing-page`  
**Date**: 2025-02-11

## Overview

This guide provides step-by-step instructions for implementing and testing the public landing page feature.

## Prerequisites

- .NET 8.0 SDK installed
- Node.js 18+ and npm installed
- PostgreSQL 15+ (via Supabase) running
- Angular CLI 21 installed (`npm install -g @angular/cli`)
- Existing Maken project cloned and set up

## Implementation Steps

### Phase 1: Backend Implementation

#### Step 1: Create ContactInquiry Entity

**File**: `src/Maken.Domain/Entities/ContactInquiry.cs`

```csharp
public class ContactInquiry : BaseEntity
{
    public string ContactName { get; private set; }
    public string Email { get; private set; }
    public string OrganizationName { get; private set; }
    public string Message { get; private set; }
    public ContactInquiryStatus Status { get; private set; }
    public DateTime SubmittedAt { get; private set; }
    public DateTime? ReviewedAt { get; private set; }
    public Guid? ReviewedBy { get; private set; }
    public string? Notes { get; private set; }

    private ContactInquiry() { } // EF Core

    public static ContactInquiry Create(string contactName, string email, 
        string organizationName, string message)
    {
        // Validation and creation logic
    }

    public void UpdateStatus(ContactInquiryStatus status, Guid? reviewedBy, string? notes)
    {
        // Status update logic
    }
}

public enum ContactInquiryStatus
{
    New = 0,
    Reviewed = 1,
    Contacted = 2
}
```

#### Step 2: Create Database Migration

```bash
cd src/Maken.Infrastructure
dotnet ef migrations add AddContactInquiryEntity --startup-project ../Maken.Api
dotnet ef database update --startup-project ../Maken.Api
```

#### Step 3: Create Application Layer Commands/Queries

**Files to create**:
- `src/Maken.Application/Commands/ContactInquiries/CreateContactInquiryCommand.cs`
- `src/Maken.Application/Commands/ContactInquiries/CreateContactInquiryCommandHandler.cs`
- `src/Maken.Application/Queries/Organizations/GetPublicOrganizationsQuery.cs`
- `src/Maken.Application/Queries/Organizations/GetPublicOrganizationsQueryHandler.cs`
- `src/Maken.Application/Queries/ContactInquiries/GetContactInquiriesQuery.cs`
- `src/Maken.Application/Queries/ContactInquiries/GetContactInquiriesQueryHandler.cs`

#### Step 4: Create API Controllers

**File**: `src/Maken.Api/Controllers/PublicController.cs`

```csharp
[ApiController]
[Route("api/public")]
[AllowAnonymous]
public class PublicController : ControllerBase
{
    [HttpGet("organizations")]
    [ResponseCache(Duration = 300)] // 5 minutes
    public async Task<ActionResult<List<PublicOrganizationResponse>>> GetOrganizations()
    {
        // Implementation
    }
}
```

**File**: `src/Maken.Api/Controllers/ContactInquiriesController.cs`

```csharp
[ApiController]
[Route("api/contact-inquiries")]
public class ContactInquiriesController : ControllerBase
{
    [HttpPost]
    [AllowAnonymous]
    [RateLimit(MaxRequests = 5, WindowMinutes = 60)]
    public async Task<ActionResult<ContactInquiryResponse>> Create(
        [FromBody] CreateContactInquiryRequest request)
    {
        // Implementation
    }

    [HttpGet]
    [Authorize(Roles = "PlatformAdmin")]
    public async Task<ActionResult<ContactInquiryListResponse>> GetAll(
        [FromQuery] ContactInquiryStatus? status,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        // Implementation
    }

    [HttpPatch("{id}/status")]
    [Authorize(Roles = "PlatformAdmin")]
    public async Task<ActionResult<ContactInquiryResponse>> UpdateStatus(
        Guid id, [FromBody] UpdateContactInquiryStatusRequest request)
    {
        // Implementation
    }
}
```

#### Step 5: Run Backend Tests

```bash
cd tests/Maken.Domain.Tests
dotnet test

cd ../Maken.Application.Tests
dotnet test

cd ../Maken.Api.Tests
dotnet test
```

### Phase 2: Frontend Implementation

#### Step 6: Create Public Module

```bash
cd frontend/src/app
ng generate module public --routing
ng generate component public/landing-page
ng generate component public/components/hero-section
ng generate component public/components/organization-showcase
ng generate component public/components/features-section
ng generate component public/components/how-it-works-section
ng generate component public/components/contact-form
ng generate service public/services/public-api
```

#### Step 7: Update App Routing

**File**: `frontend/src/app/app.routes.ts`

```typescript
export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./public/landing-page/landing-page.component')
      .then(m => m.LandingPageComponent)
  },
  {
    path: 'admin',
    canActivate: [tenantGuard, authGuard],
    loadChildren: () => import('./control-panel/control-panel.routes')
  },
  {
    path: 'app',
    canActivate: [tenantGuard, authGuard],
    loadChildren: () => import('./application/application.routes')
  },
  // ... other routes
];
```

#### Step 8: Update Tenant Guard

**File**: `frontend/src/app/core/guards/tenant.guard.ts`

```typescript
export const tenantGuard: CanActivateFn = (route, state) => {
  // Bypass tenant resolution for public routes
  if (state.url === '/' || state.url.startsWith('/public')) {
    return true;
  }
  
  // Existing tenant resolution logic for other routes
  // ...
};
```

#### Step 9: Implement Landing Page Component

**File**: `frontend/src/app/public/landing-page/landing-page.component.ts`

```typescript
@Component({
  selector: 'app-landing-page',
  standalone: true,
  imports: [
    HeroSectionComponent,
    OrganizationShowcaseComponent,
    FeaturesSectionComponent,
    HowItWorksSectionComponent,
    ContactFormComponent
  ],
  templateUrl: './landing-page.component.html',
  styleUrls: ['./landing-page.component.css']
})
export class LandingPageComponent implements OnInit {
  organizations$ = this.publicApi.getOrganizations();

  constructor(private publicApi: PublicApiService) {}

  ngOnInit(): void {
    // Load organizations
  }
}
```

#### Step 10: Implement Public API Service

**File**: `frontend/src/app/public/services/public-api.service.ts`

```typescript
@Injectable({
  providedIn: 'root'
})
export class PublicApiService {
  private readonly baseUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  getOrganizations(): Observable<PublicOrganization[]> {
    return this.http.get<PublicOrganization[]>(`${this.baseUrl}/api/public/organizations`);
  }

  submitContactInquiry(inquiry: CreateContactInquiry): Observable<ContactInquiry> {
    return this.http.post<ContactInquiry>(`${this.baseUrl}/api/contact-inquiries`, inquiry);
  }
}
```

#### Step 11: Create Admin Dashboard for Inquiries

```bash
cd frontend/src/app/control-panel
ng generate component contact-inquiries/contact-inquiry-list
ng generate component contact-inquiries/contact-inquiry-detail
```

#### Step 12: Run Frontend Tests

```bash
cd frontend
npm test
```

### Phase 3: Integration Testing

#### Step 13: Start Backend

```bash
cd src/Maken.Api
dotnet run
```

Backend should be running at `http://localhost:5292`

#### Step 14: Start Frontend

```bash
cd frontend
npm start
```

Frontend should be running at `http://localhost:4200`

#### Step 15: Manual Testing Checklist

**Landing Page (Public)**:
- [ ] Navigate to `http://localhost:4200/`
- [ ] Verify landing page loads without "tenant not resolved" error
- [ ] Verify hero section displays platform branding
- [ ] Verify organization showcase displays active organizations
- [ ] Verify organization cards are clickable
- [ ] Click organization card → redirects to tenant subdomain
- [ ] Verify features section displays platform features
- [ ] Verify "How It Works" section displays 3-4 steps
- [ ] Verify contact form is visible
- [ ] Fill and submit contact form → verify success message
- [ ] Test responsive design on mobile (320px), tablet (768px), desktop (1024px+)

**Admin Dashboard**:
- [ ] Login as PlatformAdmin
- [ ] Navigate to `/admin/contact-inquiries`
- [ ] Verify list of contact inquiries displays
- [ ] Filter by status (New/Reviewed/Contacted)
- [ ] Click inquiry → view details
- [ ] Update inquiry status → verify status changes
- [ ] Add admin notes → verify notes saved

**API Testing** (using Postman/curl):
```bash
# Get public organizations
curl http://localhost:5292/api/public/organizations

# Submit contact inquiry
curl -X POST http://localhost:5292/api/contact-inquiries \
  -H "Content-Type: application/json" \
  -d '{
    "contactName": "Test User",
    "email": "test@example.com",
    "organizationName": "Test Org",
    "message": "This is a test inquiry message."
  }'

# Get contact inquiries (requires auth token)
curl http://localhost:5292/api/contact-inquiries \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

### Phase 4: Deployment

#### Step 16: Build for Production

**Backend**:
```bash
cd src/Maken.Api
dotnet publish -c Release -o ./publish
```

**Frontend**:
```bash
cd frontend
npm run build --prod
```

#### Step 17: Deploy

Follow your deployment process for:
- Backend API to hosting service
- Frontend to static hosting or CDN
- Database migrations to production database

## Troubleshooting

### Issue: "Tenant not resolved" error still appears

**Solution**: 
- Verify tenant guard bypasses public routes
- Check app routing configuration
- Ensure landing page route is at root (`/`)

### Issue: Organizations not displaying

**Solution**:
- Verify organizations exist in database with `IsActive = true`
- Check API endpoint returns data: `curl http://localhost:5292/api/public/organizations`
- Check browser console for CORS errors

### Issue: Contact form submission fails

**Solution**:
- Check validation errors in browser console
- Verify API endpoint is accessible
- Check rate limiting (max 5 submissions per hour per IP)
- Verify database connection

### Issue: Admin dashboard not accessible

**Solution**:
- Verify user has PlatformAdmin role
- Check authentication token is valid
- Verify authorization middleware is configured

## Performance Optimization

### Backend
- Enable response caching for organization list (5 minutes)
- Add database indexes on ContactInquiry (Status, SubmittedAt, IsDeleted)
- Implement rate limiting on contact form endpoint

### Frontend
- Lazy load landing page module
- Optimize organization logos (WebP format, max 200KB)
- Implement skeleton loaders for progressive rendering
- Use Angular's OnPush change detection strategy

## Next Steps

After successful implementation:
1. Monitor contact inquiry submissions
2. Gather user feedback on landing page design
3. Plan future enhancements (email notifications, CMS for content)
4. Set up analytics tracking for landing page visits and conversions

## Support

For issues or questions:
- Check the specification: `specs/010-public-landing-page/spec.md`
- Review the data model: `specs/010-public-landing-page/data-model.md`
- Consult the API contract: `specs/010-public-landing-page/contracts/openapi.yaml`
- Contact the development team

---

**Last Updated**: 2025-02-11  
**Version**: 1.0
