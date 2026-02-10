import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { tenantGuard } from './tenant.guard';
import { TenantService } from '../services/tenant.service';
import { TenantContext } from '../models/tenant.model';
import { vi } from 'vitest';

describe('tenantGuard', () => {
  let tenantService: TenantService;
  let router: Router;

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
    const mockTenantService = {
      get isTenantResolved() { return false; },
      get currentTenant() { return null; }
    };

    TestBed.configureTestingModule({
      providers: [
        {
          provide: TenantService,
          useValue: mockTenantService
        },
        {
          provide: Router,
          useValue: {
            navigate: vi.fn()
          }
        }
      ]
    });

    tenantService = TestBed.inject(TenantService);
    router = TestBed.inject(Router);
  });

  it('should allow navigation when tenant is resolved', () => {
    // Spy on the getter to return true
    vi.spyOn(tenantService, 'isTenantResolved', 'get').mockReturnValue(true);
    vi.spyOn(tenantService, 'currentTenant', 'get').mockReturnValue(mockTenant);

    const route: any = {};
    const state: any = { url: '/app/dashboard' };

    const result = TestBed.runInInjectionContext(() => 
      tenantGuard(route, state)
    );

    expect(result).toBe(true);
    expect(router.navigate).not.toHaveBeenCalled();
  });

  it('should block navigation when tenant is not resolved', () => {
    // Spy on the getter to return false
    vi.spyOn(tenantService, 'isTenantResolved', 'get').mockReturnValue(false);
    vi.spyOn(tenantService, 'currentTenant', 'get').mockReturnValue(null);

    const route: any = {};
    const state: any = { url: '/app/dashboard' };

    const result = TestBed.runInInjectionContext(() => 
      tenantGuard(route, state)
    );

    expect(result).toBe(false);
    expect(router.navigate).toHaveBeenCalledWith(
      ['/error'],
      {
        queryParams: {
          reason: 'tenant-not-resolved',
          returnUrl: '/app/dashboard'
        }
      }
    );
  });

  it('should redirect to error page with correct query params', () => {
    vi.spyOn(tenantService, 'isTenantResolved', 'get').mockReturnValue(false);

    const route: any = {};
    const state: any = { url: '/admin/courses' };

    TestBed.runInInjectionContext(() => 
      tenantGuard(route, state)
    );

    expect(router.navigate).toHaveBeenCalledWith(
      ['/error'],
      expect.objectContaining({
        queryParams: expect.objectContaining({
          reason: 'tenant-not-resolved',
          returnUrl: '/admin/courses'
        })
      })
    );
  });

  it('should handle root path navigation', () => {
    vi.spyOn(tenantService, 'isTenantResolved', 'get').mockReturnValue(true);
    vi.spyOn(tenantService, 'currentTenant', 'get').mockReturnValue(mockTenant);

    const route: any = {};
    const state: any = { url: '/' };

    const result = TestBed.runInInjectionContext(() => 
      tenantGuard(route, state)
    );

    expect(result).toBe(true);
  });
});
