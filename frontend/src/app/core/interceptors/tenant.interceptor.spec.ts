import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { tenantInterceptor } from './tenant.interceptor';
import { TenantService } from '../services/tenant.service';
import { TenantContext } from '../models/tenant.model';

describe('tenantInterceptor', () => {
  let httpClient: HttpClient;
  let httpMock: HttpTestingController;
  let tenantService: TenantService;

  // Mock tenant data
  const mockTenant: TenantContext = {
    id: '123e4567-e89b-12d3-a456-426614174000',
    subdomain: 'academy',
    name: 'Al-Azhar Academy',
    theme: {
      primaryColor: '#1E40AF',
      secondaryColor: '#64748B',
      logoUrl: 'https://example.com/logo.png'
    },
    isActive: true
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([tenantInterceptor])),
        provideHttpClientTesting(),
        {
          provide: TenantService,
          useValue: {
            currentTenant: null
          }
        }
      ]
    });

    httpClient = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
    tenantService = TestBed.inject(TenantService);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should add X-Tenant-ID header to API requests when tenant is resolved', () => {
    // Mock tenant as resolved
    (tenantService as any).currentTenant = mockTenant;

    httpClient.get('/api/courses').subscribe();

    const req = httpMock.expectOne('/api/courses');
    expect(req.request.headers.has('X-Tenant-ID')).toBe(true);
    expect(req.request.headers.get('X-Tenant-ID')).toBe(mockTenant.id);

    req.flush({});
  });

  it('should not add X-Tenant-ID header when tenant is not resolved', () => {
    // Mock tenant as not resolved
    (tenantService as any).currentTenant = null;

    httpClient.get('/api/courses').subscribe();

    const req = httpMock.expectOne('/api/courses');
    expect(req.request.headers.has('X-Tenant-ID')).toBe(false);

    req.flush({});
  });

  it('should not add X-Tenant-ID header to non-API requests', () => {
    // Mock tenant as resolved
    (tenantService as any).currentTenant = mockTenant;

    httpClient.get('https://external-api.com/data').subscribe();

    const req = httpMock.expectOne('https://external-api.com/data');
    expect(req.request.headers.has('X-Tenant-ID')).toBe(false);

    req.flush({});
  });

  it('should add header to POST requests', () => {
    (tenantService as any).currentTenant = mockTenant;

    httpClient.post('/api/courses', { name: 'Test Course' }).subscribe();

    const req = httpMock.expectOne('/api/courses');
    expect(req.request.headers.get('X-Tenant-ID')).toBe(mockTenant.id);
    expect(req.request.method).toBe('POST');

    req.flush({});
  });

  it('should add header to PUT requests', () => {
    (tenantService as any).currentTenant = mockTenant;

    httpClient.put('/api/courses/123', { name: 'Updated Course' }).subscribe();

    const req = httpMock.expectOne('/api/courses/123');
    expect(req.request.headers.get('X-Tenant-ID')).toBe(mockTenant.id);
    expect(req.request.method).toBe('PUT');

    req.flush({});
  });

  it('should add header to DELETE requests', () => {
    (tenantService as any).currentTenant = mockTenant;

    httpClient.delete('/api/courses/123').subscribe();

    const req = httpMock.expectOne('/api/courses/123');
    expect(req.request.headers.get('X-Tenant-ID')).toBe(mockTenant.id);
    expect(req.request.method).toBe('DELETE');

    req.flush({});
  });

  it('should preserve existing headers', () => {
    (tenantService as any).currentTenant = mockTenant;

    httpClient.get('/api/courses', {
      headers: {
        'Authorization': 'Bearer token123',
        'Content-Type': 'application/json'
      }
    }).subscribe();

    const req = httpMock.expectOne('/api/courses');
    expect(req.request.headers.get('X-Tenant-ID')).toBe(mockTenant.id);
    expect(req.request.headers.get('Authorization')).toBe('Bearer token123');
    expect(req.request.headers.get('Content-Type')).toBe('application/json');

    req.flush({});
  });

  it('should handle multiple API requests with same tenant', () => {
    (tenantService as any).currentTenant = mockTenant;

    httpClient.get('/api/courses').subscribe();
    httpClient.get('/api/lessons').subscribe();

    const req1 = httpMock.expectOne('/api/courses');
    const req2 = httpMock.expectOne('/api/lessons');

    expect(req1.request.headers.get('X-Tenant-ID')).toBe(mockTenant.id);
    expect(req2.request.headers.get('X-Tenant-ID')).toBe(mockTenant.id);

    req1.flush({});
    req2.flush({});
  });
});
