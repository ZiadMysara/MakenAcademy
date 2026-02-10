import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { authGuard } from './auth.guard';
import { AuthService } from '../services/auth.service';
import { User, UserRole } from '../models/auth.model';
import { vi } from 'vitest';

describe('authGuard', () => {
  let authService: AuthService;
  let router: Router;

  const mockUser: User = {
    id: 'user-123',
    email: 'test@example.com',
    name: 'Test User',
    tenantId: 'tenant-123'
  };

  beforeEach(() => {
    const mockAuthService = {
      get isAuthenticated() { return false; },
      get currentUser() { return null; }
    };

    TestBed.configureTestingModule({
      providers: [
        {
          provide: AuthService,
          useValue: mockAuthService
        },
        {
          provide: Router,
          useValue: {
            navigate: vi.fn()
          }
        }
      ]
    });

    authService = TestBed.inject(AuthService);
    router = TestBed.inject(Router);
  });

  it('should allow navigation when user is authenticated', () => {
    vi.spyOn(authService, 'isAuthenticated', 'get').mockReturnValue(true);
    vi.spyOn(authService, 'currentUser', 'get').mockReturnValue(mockUser);

    const route: any = {};
    const state: any = { url: '/app/dashboard' };

    const result = TestBed.runInInjectionContext(() => 
      authGuard(route, state)
    );

    expect(result).toBe(true);
    expect(router.navigate).not.toHaveBeenCalled();
  });

  it('should block navigation when user is not authenticated', () => {
    vi.spyOn(authService, 'isAuthenticated', 'get').mockReturnValue(false);
    vi.spyOn(authService, 'currentUser', 'get').mockReturnValue(null);

    const route: any = {};
    const state: any = { url: '/app/dashboard' };

    const result = TestBed.runInInjectionContext(() => 
      authGuard(route, state)
    );

    expect(result).toBe(false);
    expect(router.navigate).toHaveBeenCalledWith(
      ['/login'],
      {
        queryParams: {
          returnUrl: '/app/dashboard'
        }
      }
    );
  });

  it('should redirect to login with return URL', () => {
    vi.spyOn(authService, 'isAuthenticated', 'get').mockReturnValue(false);

    const route: any = {};
    const state: any = { url: '/admin/courses' };

    TestBed.runInInjectionContext(() => 
      authGuard(route, state)
    );

    expect(router.navigate).toHaveBeenCalledWith(
      ['/login'],
      expect.objectContaining({
        queryParams: expect.objectContaining({
          returnUrl: '/admin/courses'
        })
      })
    );
  });

  it('should handle root path navigation', () => {
    vi.spyOn(authService, 'isAuthenticated', 'get').mockReturnValue(true);
    vi.spyOn(authService, 'currentUser', 'get').mockReturnValue(mockUser);

    const route: any = {};
    const state: any = { url: '/' };

    const result = TestBed.runInInjectionContext(() => 
      authGuard(route, state)
    );

    expect(result).toBe(true);
  });

  it('should not make authorization decisions based on user role', () => {
    // Guard should only check authentication, not authorization
    const userWithRole = { ...mockUser, role: UserRole.Student };
    
    vi.spyOn(authService, 'isAuthenticated', 'get').mockReturnValue(true);
    vi.spyOn(authService, 'currentUser', 'get').mockReturnValue(userWithRole);

    const route: any = {};
    const state: any = { url: '/admin/courses' }; // Admin route

    const result = TestBed.runInInjectionContext(() => 
      authGuard(route, state)
    );

    // Guard allows navigation regardless of role
    // Authorization is handled by backend
    expect(result).toBe(true);
  });
});
