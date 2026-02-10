import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { Router } from '@angular/router';
import { authInterceptor } from './auth.interceptor';
import { AuthService } from '../services/auth.service';
import { AuthState, User, LoginResponse } from '../models/auth.model';
import { vi } from 'vitest';
import { of, throwError } from 'rxjs';

describe('authInterceptor', () => {
  let httpClient: HttpClient;
  let httpMock: HttpTestingController;
  let authService: AuthService;
  let router: Router;

  const mockUser: User = {
    id: 'user-123',
    email: 'test@example.com',
    name: 'Test User',
    tenantId: 'tenant-123'
  };

  const mockAuthState: AuthState = {
    isAuthenticated: true,
    user: mockUser,
    token: 'mock-access-token',
    role: null
  };

  const mockLoginResponse: LoginResponse = {
    user: mockUser,
    token: 'new-access-token',
    refreshToken: 'new-refresh-token',
    expiresIn: 3600
  };

  beforeEach(() => {
    const mockAuthService = {
      get currentAuthState() { return mockAuthState; },
      get isAuthenticated() { return mockAuthState.isAuthenticated; },
      refreshToken: vi.fn(),
      logout: vi.fn()
    };

    const mockRouter = {
      navigate: vi.fn()
    };

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: mockAuthService },
        { provide: Router, useValue: mockRouter }
      ]
    });

    httpClient = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
    authService = TestBed.inject(AuthService);
    router = TestBed.inject(Router);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should add Authorization header to API requests when authenticated', () => {
    httpClient.get('/api/courses').subscribe();

    const req = httpMock.expectOne('/api/courses');
    expect(req.request.headers.has('Authorization')).toBe(true);
    expect(req.request.headers.get('Authorization')).toBe('Bearer mock-access-token');

    req.flush({});
  });

  it('should not add Authorization header when not authenticated', () => {
    // Mock as not authenticated
    vi.spyOn(authService, 'currentAuthState', 'get').mockReturnValue({
      isAuthenticated: false,
      user: null,
      token: null,
      role: null
    });
    vi.spyOn(authService, 'isAuthenticated', 'get').mockReturnValue(false);

    httpClient.get('/api/courses').subscribe();

    const req = httpMock.expectOne('/api/courses');
    expect(req.request.headers.has('Authorization')).toBe(false);

    req.flush({});
  });

  it('should not add Authorization header to non-API requests', () => {
    httpClient.get('https://external-api.com/data').subscribe();

    const req = httpMock.expectOne('https://external-api.com/data');
    expect(req.request.headers.has('Authorization')).toBe(false);

    req.flush({});
  });

  it('should attempt token refresh on 401 error', async () => {
    // Mock refresh token to return new token
    vi.spyOn(authService, 'refreshToken').mockReturnValue(of(mockLoginResponse));
    
    // Track state changes
    let tokenBeforeRefresh = mockAuthState.token;
    let tokenAfterRefresh = 'new-access-token';
    
    // Mock currentAuthState to return different values
    let callCount = 0;
    vi.spyOn(authService, 'currentAuthState', 'get').mockImplementation(() => {
      callCount++;
      if (callCount === 1) {
        return { ...mockAuthState, token: tokenBeforeRefresh };
      } else {
        return { ...mockAuthState, token: tokenAfterRefresh };
      }
    });

    const promise = httpClient.get('/api/courses').toPromise();

    // First request with old token
    const req1 = httpMock.expectOne('/api/courses');
    expect(req1.request.headers.get('Authorization')).toBe('Bearer mock-access-token');
    req1.flush('Unauthorized', { status: 401, statusText: 'Unauthorized' });

    // Retry request with new token
    const req2 = httpMock.expectOne('/api/courses');
    expect(req2.request.headers.get('Authorization')).toBe('Bearer new-access-token');
    req2.flush({ data: 'success' });

    const response = await promise;
    expect(response).toEqual({ data: 'success' });
    expect(authService.refreshToken).toHaveBeenCalled();
  });

  it('should logout and redirect on refresh failure', async () => {
    // Mock refresh token to fail
    vi.spyOn(authService, 'refreshToken').mockReturnValue(
      throwError(() => new Error('Refresh failed'))
    );

    const promise = httpClient.get('/api/courses').toPromise();

    // First request
    const req = httpMock.expectOne('/api/courses');
    req.flush('Unauthorized', { status: 401, statusText: 'Unauthorized' });

    try {
      await promise;
      throw new Error('Should have thrown');
    } catch (error) {
      expect(authService.logout).toHaveBeenCalled();
      expect(router.navigate).toHaveBeenCalledWith(['/login']);
    }
  });

  it('should not attempt refresh on 401 when not authenticated', async () => {
    vi.spyOn(authService, 'currentAuthState', 'get').mockReturnValue({
      isAuthenticated: false,
      user: null,
      token: null,
      role: null
    });
    vi.spyOn(authService, 'isAuthenticated', 'get').mockReturnValue(false);
    vi.spyOn(authService, 'refreshToken');

    const promise = httpClient.get('/api/courses').toPromise();

    const req = httpMock.expectOne('/api/courses');
    req.flush('Unauthorized', { status: 401, statusText: 'Unauthorized' });

    try {
      await promise;
      throw new Error('Should have thrown');
    } catch (error) {
      expect(authService.refreshToken).not.toHaveBeenCalled();
    }
  });

  it('should pass through non-401 errors', async () => {
    const promise = httpClient.get('/api/courses').toPromise();

    const req = httpMock.expectOne('/api/courses');
    req.flush('Not Found', { status: 404, statusText: 'Not Found' });

    try {
      await promise;
      throw new Error('Should have thrown');
    } catch (error: any) {
      expect(error.status).toBe(404);
      expect(authService.refreshToken).not.toHaveBeenCalled();
    }
  });

  it('should add header to POST requests', () => {
    httpClient.post('/api/courses', { name: 'Test Course' }).subscribe();

    const req = httpMock.expectOne('/api/courses');
    expect(req.request.headers.get('Authorization')).toBe('Bearer mock-access-token');
    expect(req.request.method).toBe('POST');

    req.flush({});
  });

  it('should preserve existing headers', () => {
    httpClient.get('/api/courses', {
      headers: {
        'Content-Type': 'application/json',
        'X-Custom-Header': 'custom-value'
      }
    }).subscribe();

    const req = httpMock.expectOne('/api/courses');
    expect(req.request.headers.get('Authorization')).toBe('Bearer mock-access-token');
    expect(req.request.headers.get('Content-Type')).toBe('application/json');
    expect(req.request.headers.get('X-Custom-Header')).toBe('custom-value');

    req.flush({});
  });
});
