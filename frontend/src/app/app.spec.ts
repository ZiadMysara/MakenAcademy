import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { Location } from '@angular/common';
import { provideRouter } from '@angular/router';
import { App } from './app';
import { routes } from './app.routes';
import { TenantService } from './core/services/tenant.service';
import { AuthService } from './core/services/auth.service';
import { of } from 'rxjs';

/**
 * Root App Component Tests
 * Constitution Rules: FE-034, FE-035 (surface isolation)
 * 
 * Tests:
 * - Component creation
 * - Surface isolation (admin vs app routes)
 * - Lazy loading of surface modules
 * - Shared services are singleton across surfaces
 */
describe('App', () => {
  let mockTenantService: any;
  let mockAuthService: any;

  beforeEach(async () => {
    // Mock services to bypass guards for routing tests
    mockTenantService = {
      tenant$: of({
        id: 'test-tenant-id',
        subdomain: 'test',
        name: 'Test Tenant',
        theme: {},
        isActive: true
      }),
      get isTenantResolved() { return true; },
      get currentTenant() {
        return {
          id: 'test-tenant-id',
          subdomain: 'test',
          name: 'Test Tenant',
          theme: {},
          isActive: true
        };
      }
    };

    mockAuthService = {
      authState$: of({
        isAuthenticated: true,
        user: {
          id: 'test-user',
          email: 'test@example.com',
          name: 'Test User',
          tenantId: 'test-tenant-id'
        },
        token: 'test-token',
        role: null
      }),
      get isAuthenticated() { return true; },
      get currentUser() {
        return {
          id: 'test-user',
          email: 'test@example.com',
          name: 'Test User',
          tenantId: 'test-tenant-id'
        };
      }
    };

    await TestBed.configureTestingModule({
      imports: [App],
      providers: [
        provideRouter(routes),
        { provide: TenantService, useValue: mockTenantService },
        { provide: AuthService, useValue: mockAuthService }
      ]
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('should have router outlet', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('router-outlet')).toBeTruthy();
  });

  describe('Surface Isolation (FE-034, FE-035)', () => {
    it('should lazy-load Control Panel routes under /admin', async () => {
      const fixture = TestBed.createComponent(App);
      const router = TestBed.inject(Router);
      const location = TestBed.inject(Location);

      await router.navigate(['/admin']);
      // Redirects to /admin/dashboard (default child route)
      expect(location.path()).toBe('/admin/dashboard');
    });

    it('should lazy-load Application routes under /app', async () => {
      const fixture = TestBed.createComponent(App);
      const router = TestBed.inject(Router);
      const location = TestBed.inject(Location);

      await router.navigate(['/app']);
      // Redirects to /app/home (default child route)
      expect(location.path()).toBe('/app/home');
    });

    it('should redirect root path to /app', async () => {
      const fixture = TestBed.createComponent(App);
      const router = TestBed.inject(Router);
      const location = TestBed.inject(Location);

      await router.navigate(['/']);
      // Redirects to /app, then to /app/home (default child route)
      expect(location.path()).toBe('/app/home');
    });

    it('should redirect unknown paths to /error', async () => {
      const fixture = TestBed.createComponent(App);
      const router = TestBed.inject(Router);
      const location = TestBed.inject(Location);

      await router.navigate(['/unknown-path']);
      expect(location.path()).toBe('/error');
    });

    it('should use singleton TenantService across surfaces', () => {
      const fixture = TestBed.createComponent(App);
      const tenantService1 = TestBed.inject(TenantService);
      const tenantService2 = TestBed.inject(TenantService);
      
      // Same instance across the app
      expect(tenantService1).toBe(tenantService2);
    });

    it('should use singleton AuthService across surfaces', () => {
      const fixture = TestBed.createComponent(App);
      const authService1 = TestBed.inject(AuthService);
      const authService2 = TestBed.inject(AuthService);
      
      // Same instance across the app
      expect(authService1).toBe(authService2);
    });
  });
});
