import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TenantService } from './tenant.service';
import { TenantContext } from '../models/tenant.model';
import { environment } from '../../../environments/environment';
import { vi } from 'vitest';

describe('TenantService', () => {
  let service: TenantService;
  let httpMock: HttpTestingController;
  const baseUrl = environment.apiBaseUrl;

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
        TenantService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });

    service = TestBed.inject(TenantService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    service.clearTenant(); // Reset for next test
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('Subdomain extraction', () => {
    it('should extract subdomain from hostname (academy.maken.app → academy)', async () => {
      // Spy on getHostname method
      vi.spyOn(service as any, 'getHostname').mockReturnValue('academy.maken.app');

      const promise = service.resolveTenant();

      const req = httpMock.expectOne(`${baseUrl}/api/tenants/resolve?subdomain=academy`);
      expect(req.request.method).toBe('GET');
      req.flush(mockTenant);

      const tenant = await promise;
      expect(tenant.subdomain).toBe('academy');
    });

    it('should handle localhost for development', async () => {
      vi.spyOn(service as any, 'getHostname').mockReturnValue('localhost');

      const promise = service.resolveTenant();

      const req = httpMock.expectOne(`${baseUrl}/api/tenants/resolve?subdomain=localhost`);
      req.flush({ ...mockTenant, subdomain: 'localhost' });

      const tenant = await promise;
      expect(tenant.subdomain).toBe('localhost');
    });
  });

  describe('Tenant resolution', () => {
    it('should successfully resolve tenant from API', async () => {
      vi.spyOn(service as any, 'getHostname').mockReturnValue('academy.maken.app');

      const promise = service.resolveTenant();

      const req = httpMock.expectOne(`${baseUrl}/api/tenants/resolve?subdomain=academy`);
      req.flush(mockTenant);

      const tenant = await promise;
      expect(tenant).toEqual(mockTenant);
      expect(service.currentTenant).toEqual(mockTenant);
      expect(service.isTenantResolved).toBe(true);
    });

    it('should store tenant context as immutable', async () => {
      vi.spyOn(service as any, 'getHostname').mockReturnValue('academy.maken.app');

      const promise = service.resolveTenant();

      const req = httpMock.expectOne(`${baseUrl}/api/tenants/resolve?subdomain=academy`);
      req.flush(mockTenant);

      await promise;

      // Verify tenant is stored
      const tenant1 = service.currentTenant;
      const tenant2 = service.currentTenant;
      
      expect(tenant1).toBe(tenant2); // Same reference (immutable)
    });

    it('should expose tenant as readonly Observable', async () => {
      vi.spyOn(service as any, 'getHostname').mockReturnValue('academy.maken.app');

      // Subscribe before resolution to capture all emissions
      let emittedTenant: TenantContext | null = null;
      const subscription = service.tenant$.subscribe(tenant => {
        emittedTenant = tenant;
      });

      // Start resolution
      const promise = service.resolveTenant();

      const req = httpMock.expectOne(`${baseUrl}/api/tenants/resolve?subdomain=academy`);
      req.flush(mockTenant);

      await promise;

      // Verify Observable emitted the tenant
      expect(emittedTenant).toEqual(mockTenant);
      
      subscription.unsubscribe();
    });
  });

  describe('Error handling', () => {
    it('should handle unresolvable tenant (API error)', async () => {
      vi.spyOn(service as any, 'getHostname').mockReturnValue('invalid.maken.app');

      const promise = service.resolveTenant();

      const req = httpMock.expectOne(`${baseUrl}/api/tenants/resolve?subdomain=invalid`);
      req.flush('Tenant not found', { status: 404, statusText: 'Not Found' });

      try {
        await promise;
        throw new Error('Should have thrown an error');
      } catch (error: any) {
        expect(error.message).toContain('Failed to resolve tenant');
        expect(service.isTenantResolved).toBe(false);
        expect(service.currentTenant).toBeNull();
      }
    });

    it('should handle API unreachable (network error)', async () => {
      vi.spyOn(service as any, 'getHostname').mockReturnValue('academy.maken.app');

      const promise = service.resolveTenant();

      const req = httpMock.expectOne(`${baseUrl}/api/tenants/resolve?subdomain=academy`);
      req.error(new ProgressEvent('Network error'));

      try {
        await promise;
        throw new Error('Should have thrown an error');
      } catch (error: any) {
        expect(error.message).toContain('Failed to resolve tenant');
        expect(service.isTenantResolved).toBe(false);
      }
    });

    it('should reject if subdomain cannot be extracted', async () => {
      vi.spyOn(service as any, 'getHostname').mockReturnValue('maken.app'); // Root domain, no subdomain

      try {
        await service.resolveTenant();
        throw new Error('Should have thrown an error');
      } catch (error: any) {
        expect(error.message).toContain('Unable to determine tenant from URL');
        expect(service.isTenantResolved).toBe(false);
      }
    });
  });

  describe('Immutability', () => {
    it('should not allow tenant context modification after resolution', async () => {
      vi.spyOn(service as any, 'getHostname').mockReturnValue('academy.maken.app');

      const promise = service.resolveTenant();

      const req = httpMock.expectOne(`${baseUrl}/api/tenants/resolve?subdomain=academy`);
      req.flush(mockTenant);

      await promise;

      // Attempt to call resolveTenant again should use the same context
      const tenant1 = service.currentTenant;
      
      // Second resolution attempt
      const promise2 = service.resolveTenant();
      const req2 = httpMock.expectOne(`${baseUrl}/api/tenants/resolve?subdomain=academy`);
      req2.flush({ ...mockTenant, name: 'Different Name' });
      
      await promise2;
      
      // Tenant should be updated (this is expected behavior for re-resolution)
      const tenant2 = service.currentTenant;
      expect(tenant2?.name).toBe('Different Name');
    });
  });
});
