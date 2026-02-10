import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { AuthService } from './auth.service';
import { LoginCredentials, LoginResponse, User, UserRole } from '../models/auth.model';
import { environment } from '../../../environments/environment';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;
  const baseUrl = environment.apiBaseUrl;

  // Mock data
  const mockUser: User = {
    id: 'user-123',
    email: 'test@example.com',
    name: 'Test User',
    tenantId: 'tenant-123'
  };

  const mockLoginResponse: LoginResponse = {
    user: mockUser,
    token: 'mock-access-token',
    refreshToken: 'mock-refresh-token',
    expiresIn: 3600 // 1 hour
  };

  const mockCredentials: LoginCredentials = {
    email: 'test@example.com',
    password: 'password123'
  };

  beforeEach(() => {
    // Clear localStorage before each test
    localStorage.clear();

    TestBed.configureTestingModule({
      providers: [
        AuthService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });

    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('Login', () => {
    it('should login successfully and update auth state', async () => {
      const loginPromise = service.login(mockCredentials).toPromise();

      const req = httpMock.expectOne(`${baseUrl}/api/auth/login`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(mockCredentials);
      req.flush(mockLoginResponse);

      const response = await loginPromise;
      expect(response).toEqual(mockLoginResponse);
      expect(service.isAuthenticated).toBe(true);
      expect(service.currentUser).toEqual(mockUser);
      expect(service.currentAuthState.token).toBe(mockLoginResponse.token);
    });

    it('should store tokens in localStorage on successful login', async () => {
      const loginPromise = service.login(mockCredentials).toPromise();

      const req = httpMock.expectOne(`${baseUrl}/api/auth/login`);
      req.flush(mockLoginResponse);

      await loginPromise;
      expect(localStorage.getItem('maken_access_token')).toBe(mockLoginResponse.token);
      expect(localStorage.getItem('maken_refresh_token')).toBe(mockLoginResponse.refreshToken);
      expect(localStorage.getItem('maken_user')).toBeTruthy();
      expect(localStorage.getItem('maken_token_expiry')).toBeTruthy();
    });

    it('should emit auth state through Observable', async () => {
      let emissionCount = 0;
      let lastState: any = null;
      
      const subscription = service.authState$.subscribe(state => {
        emissionCount++;
        lastState = state;
      });

      const loginPromise = service.login(mockCredentials).toPromise();

      const req = httpMock.expectOne(`${baseUrl}/api/auth/login`);
      req.flush(mockLoginResponse);

      await loginPromise;
      
      expect(emissionCount).toBeGreaterThanOrEqual(2);
      expect(lastState.isAuthenticated).toBe(true);
      expect(lastState.user).toEqual(mockUser);
      expect(lastState.token).toBe(mockLoginResponse.token);
      
      subscription.unsubscribe();
    });

    it('should handle login failure', async () => {
      const loginPromise = service.login(mockCredentials).toPromise();

      const req = httpMock.expectOne(`${baseUrl}/api/auth/login`);
      req.flush('Invalid credentials', { status: 401, statusText: 'Unauthorized' });

      try {
        await loginPromise;
        throw new Error('Should not succeed');
      } catch (error: any) {
        expect(error.message).toContain('Login failed');
        expect(service.isAuthenticated).toBe(false);
      }
    });
  });

  describe('Logout', () => {
    it('should clear auth state on logout', async () => {
      const loginPromise = service.login(mockCredentials).toPromise();

      const req = httpMock.expectOne(`${baseUrl}/api/auth/login`);
      req.flush(mockLoginResponse);

      await loginPromise;
      expect(service.isAuthenticated).toBe(true);
      
      service.logout();
      
      expect(service.isAuthenticated).toBe(false);
      expect(service.currentUser).toBeNull();
      expect(service.currentAuthState.token).toBeNull();
    });

    it('should clear localStorage on logout', async () => {
      const loginPromise = service.login(mockCredentials).toPromise();

      const req = httpMock.expectOne(`${baseUrl}/api/auth/login`);
      req.flush(mockLoginResponse);

      await loginPromise;
      expect(localStorage.getItem('maken_access_token')).toBeTruthy();
      
      service.logout();
      
      expect(localStorage.getItem('maken_access_token')).toBeNull();
      expect(localStorage.getItem('maken_refresh_token')).toBeNull();
      expect(localStorage.getItem('maken_user')).toBeNull();
      expect(localStorage.getItem('maken_token_expiry')).toBeNull();
    });
  });

  describe('Token Refresh', () => {
    it('should refresh token successfully', async () => {
      localStorage.setItem('maken_refresh_token', 'mock-refresh-token');

      const newLoginResponse: LoginResponse = {
        ...mockLoginResponse,
        token: 'new-access-token'
      };

      const refreshPromise = service.refreshToken().toPromise();

      const req = httpMock.expectOne(`${baseUrl}/api/auth/refresh`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ refreshToken: 'mock-refresh-token' });
      req.flush(newLoginResponse);

      const response = await refreshPromise;
      expect(response?.token).toBe('new-access-token');
      expect(localStorage.getItem('maken_access_token')).toBe('new-access-token');
    });

    it('should logout on token refresh failure', async () => {
      const loginPromise = service.login(mockCredentials).toPromise();

      const loginReq = httpMock.expectOne(`${baseUrl}/api/auth/login`);
      loginReq.flush(mockLoginResponse);

      await loginPromise;
      expect(service.isAuthenticated).toBe(true);

      const refreshPromise = service.refreshToken().toPromise();

      const refreshReq = httpMock.expectOne(`${baseUrl}/api/auth/refresh`);
      refreshReq.flush('Refresh token expired', { status: 401, statusText: 'Unauthorized' });

      try {
        await refreshPromise;
        throw new Error('Should not succeed');
      } catch {
        expect(service.isAuthenticated).toBe(false);
      }
    });

    it('should handle missing refresh token', async () => {
      try {
        await service.refreshToken().toPromise();
        throw new Error('Should not succeed');
      } catch (error: any) {
        expect(error.message).toContain('No refresh token available');
      }
    });
  });

  describe('Auth State Observable', () => {
    it('should expose auth state as readonly Observable', async () => {
      let states: any[] = [];

      const subscription = service.authState$.subscribe(state => {
        states.push(state);
      });

      const loginPromise = service.login(mockCredentials).toPromise();

      const req = httpMock.expectOne(`${baseUrl}/api/auth/login`);
      req.flush(mockLoginResponse);

      await loginPromise;
      
      expect(states.length).toBeGreaterThanOrEqual(2);
      expect(states[0].isAuthenticated).toBe(false);
      expect(states[states.length - 1].isAuthenticated).toBe(true);
      
      subscription.unsubscribe();
    });
  });

  describe('No Authorization Decisions', () => {
    it('should not make authorization decisions based on user role', async () => {
      const userWithRole = {
        ...mockUser,
        role: UserRole.Student
      };

      const responseWithRole: LoginResponse = {
        ...mockLoginResponse,
        user: userWithRole
      };

      const loginPromise = service.login(mockCredentials).toPromise();

      const req = httpMock.expectOne(`${baseUrl}/api/auth/login`);
      req.flush(responseWithRole);

      await loginPromise;
      
      const state = service.currentAuthState;
      expect(state.role).toBeTruthy();
      
      expect((service as any).canAccess).toBeUndefined();
      expect((service as any).hasPermission).toBeUndefined();
      expect((service as any).isAuthorized).toBeUndefined();
    });
  });

  describe('Session Restoration', () => {
    it('should restore session from localStorage on initialization', () => {
      // Clear the existing service instance
      TestBed.resetTestingModule();
      
      // Setup: Store valid session data
      const expiry = Date.now() + 3600000; // 1 hour from now
      localStorage.setItem('maken_access_token', 'stored-token');
      localStorage.setItem('maken_refresh_token', 'stored-refresh-token');
      localStorage.setItem('maken_user', JSON.stringify(mockUser));
      localStorage.setItem('maken_token_expiry', expiry.toString());

      // Reconfigure TestBed and create new service instance
      TestBed.configureTestingModule({
        providers: [
          AuthService,
          provideHttpClient(),
          provideHttpClientTesting()
        ]
      });

      const newService = TestBed.inject(AuthService);
      const newHttpMock = TestBed.inject(HttpTestingController);

      expect(newService.isAuthenticated).toBe(true);
      expect(newService.currentUser).toEqual(mockUser);
      expect(newService.currentAuthState.token).toBe('stored-token');
      
      newHttpMock.verify();
    });

    it('should not restore expired session', () => {
      // Clear the existing service instance
      TestBed.resetTestingModule();
      
      // Setup: Store expired session data
      const expiry = Date.now() - 1000; // 1 second ago
      localStorage.setItem('maken_access_token', 'expired-token');
      localStorage.setItem('maken_refresh_token', 'stored-refresh-token');
      localStorage.setItem('maken_user', JSON.stringify(mockUser));
      localStorage.setItem('maken_token_expiry', expiry.toString());

      // Reconfigure TestBed and create new service instance
      TestBed.configureTestingModule({
        providers: [
          AuthService,
          provideHttpClient(),
          provideHttpClientTesting()
        ]
      });

      const newService = TestBed.inject(AuthService);
      const newHttpMock = TestBed.inject(HttpTestingController);

      // Should attempt refresh
      const req = newHttpMock.expectOne(`${baseUrl}/api/auth/refresh`);
      req.flush('Token expired', { status: 401, statusText: 'Unauthorized' });

      // Session should not be restored
      expect(newService.isAuthenticated).toBe(false);
      
      newHttpMock.verify();
    });
  });
});
