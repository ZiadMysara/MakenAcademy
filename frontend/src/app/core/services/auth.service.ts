import { Injectable, inject } from '@angular/core';
import { BehaviorSubject, Observable, throwError, timer } from 'rxjs';
import { catchError, tap, switchMap } from 'rxjs/operators';
import { AuthState, LoginCredentials, LoginResponse, User, UserRole } from '../models/auth.model';
import { ApiService } from './api.service';

/**
 * Authentication Service
 * Manages authentication lifecycle without authorization decisions
 * Constitution Rules: FE-020, FE-021, FE-022, FE-023, FE-024, FE-025, FE-026
 * 
 * Key Responsibilities:
 * - Email/password login via Supabase Auth REST API (No OAuth per Patch I)
 * - Logout and session cleanup
 * - Token storage, expiry detection, transparent refresh
 * - Expose auth state as readonly Observable (Simplicity Rule)
 * - NO authorization or business decisions
 * 
 * Important:
 * - This service manages STATE only, not authorization
 * - Authorization decisions are made by the backend
 * - Frontend respects backend responses without interpretation
 */
@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly apiService = inject(ApiService);
  
  // Authentication state - BehaviorSubject for Simplicity Rule
  private readonly authStateSubject = new BehaviorSubject<AuthState>({
    isAuthenticated: false,
    user: null,
    token: null,
    role: null
  });
  
  // Readonly Observable for external consumption
  public readonly authState$: Observable<AuthState> = this.authStateSubject.asObservable();
  
  // Token refresh timer subscription
  private refreshTimerSubscription: any = null;
  
  // Storage keys
  private readonly TOKEN_KEY = 'maken_access_token';
  private readonly REFRESH_TOKEN_KEY = 'maken_refresh_token';
  private readonly USER_KEY = 'maken_user';
  private readonly TOKEN_EXPIRY_KEY = 'maken_token_expiry';

  constructor() {
    // Restore session from localStorage on service initialization
    this.restoreSession();
  }

  /**
   * Get current authentication state (synchronous)
   */
  get currentAuthState(): AuthState {
    return this.authStateSubject.value;
  }

  /**
   * Check if user is authenticated
   */
  get isAuthenticated(): boolean {
    return this.authStateSubject.value.isAuthenticated;
  }

  /**
   * Get current user (null if not authenticated)
   */
  get currentUser(): User | null {
    return this.authStateSubject.value.user;
  }

  /**
   * Login with email and password
   * Constitution Rule: FE-026 (email/password only for MVP)
   * 
   * @param credentials Email and password
   * @returns Observable of login response
   */
  login(credentials: LoginCredentials): Observable<LoginResponse> {
    return this.apiService
      .post<LoginResponse>('api/auth/login', credentials)
      .pipe(
        tap(response => {
          // Store tokens and user data
          this.storeAuthData(response);
          
          // Update auth state
          this.updateAuthState({
            isAuthenticated: true,
            user: response.user,
            token: response.token,
            role: this.getUserRole(response.user)
          });
          
          // Schedule token refresh
          this.scheduleTokenRefresh(response.expiresIn);
        }),
        catchError(error => {
          console.error('Login failed:', error);
          return throwError(() => new Error('Login failed. Please check your credentials.'));
        })
      );
  }

  /**
   * Logout and clear session
   * Constitution Rule: FE-020 (manage auth lifecycle)
   */
  logout(): void {
    // Clear stored data
    this.clearAuthData();
    
    // Cancel token refresh timer
    if (this.refreshTimerSubscription) {
      this.refreshTimerSubscription.unsubscribe();
      this.refreshTimerSubscription = null;
    }
    
    // Update auth state
    this.updateAuthState({
      isAuthenticated: false,
      user: null,
      token: null,
      role: null
    });
  }

  /**
   * Refresh access token using refresh token
   * Constitution Rule: FE-025 (transparent token refresh)
   * 
   * @returns Observable of new login response
   */
  refreshToken(): Observable<LoginResponse> {
    const refreshToken = this.getStoredRefreshToken();
    
    if (!refreshToken) {
      return throwError(() => new Error('No refresh token available'));
    }
    
    return this.apiService
      .post<LoginResponse>('api/auth/refresh', { refreshToken })
      .pipe(
        tap(response => {
          // Store new tokens
          this.storeAuthData(response);
          
          // Update auth state with new token
          this.updateAuthState({
            ...this.authStateSubject.value,
            token: response.token
          });
          
          // Schedule next refresh
          this.scheduleTokenRefresh(response.expiresIn);
        }),
        catchError(error => {
          console.error('Token refresh failed:', error);
          // Logout on refresh failure
          this.logout();
          return throwError(() => new Error('Session expired. Please log in again.'));
        })
      );
  }

  /**
   * Restore session from localStorage
   * Called on service initialization
   */
  private restoreSession(): void {
    const token = this.getStoredToken();
    const user = this.getStoredUser();
    const expiry = this.getStoredTokenExpiry();
    
    if (token && user && expiry) {
      const now = Date.now();
      
      // Check if token is still valid
      if (expiry > now) {
        // Restore auth state
        this.updateAuthState({
          isAuthenticated: true,
          user,
          token,
          role: this.getUserRole(user)
        });
        
        // Schedule token refresh
        const expiresInSeconds = Math.floor((expiry - now) / 1000);
        this.scheduleTokenRefresh(expiresInSeconds);
      } else {
        // Token expired - attempt refresh
        this.refreshToken().subscribe({
          error: () => {
            // Refresh failed - clear session
            this.clearAuthData();
          }
        });
      }
    }
  }

  /**
   * Schedule automatic token refresh
   * Refreshes 5 minutes before expiry
   * 
   * @param expiresInSeconds Token expiry in seconds
   */
  private scheduleTokenRefresh(expiresInSeconds: number): void {
    // Cancel existing timer
    if (this.refreshTimerSubscription) {
      this.refreshTimerSubscription.unsubscribe();
    }
    
    // Refresh 5 minutes (300 seconds) before expiry
    const refreshInSeconds = Math.max(expiresInSeconds - 300, 60); // At least 60 seconds
    const refreshInMs = refreshInSeconds * 1000;
    
    this.refreshTimerSubscription = timer(refreshInMs)
      .pipe(
        switchMap(() => this.refreshToken())
      )
      .subscribe({
        error: (error) => {
          console.error('Automatic token refresh failed:', error);
        }
      });
  }

  /**
   * Update authentication state
   * 
   * @param state New auth state
   */
  private updateAuthState(state: AuthState): void {
    this.authStateSubject.next(state);
  }

  /**
   * Store authentication data in localStorage
   * 
   * @param response Login response from API
   */
  private storeAuthData(response: LoginResponse): void {
    const expiry = Date.now() + (response.expiresIn * 1000);
    
    localStorage.setItem(this.TOKEN_KEY, response.token);
    localStorage.setItem(this.REFRESH_TOKEN_KEY, response.refreshToken);
    localStorage.setItem(this.USER_KEY, JSON.stringify(response.user));
    localStorage.setItem(this.TOKEN_EXPIRY_KEY, expiry.toString());
  }

  /**
   * Clear authentication data from localStorage
   */
  private clearAuthData(): void {
    localStorage.removeItem(this.TOKEN_KEY);
    localStorage.removeItem(this.REFRESH_TOKEN_KEY);
    localStorage.removeItem(this.USER_KEY);
    localStorage.removeItem(this.TOKEN_EXPIRY_KEY);
  }

  /**
   * Get stored access token
   */
  private getStoredToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  /**
   * Get stored refresh token
   */
  private getStoredRefreshToken(): string | null {
    return localStorage.getItem(this.REFRESH_TOKEN_KEY);
  }

  /**
   * Get stored user data
   */
  private getStoredUser(): User | null {
    const userJson = localStorage.getItem(this.USER_KEY);
    if (userJson) {
      try {
        return JSON.parse(userJson);
      } catch {
        return null;
      }
    }
    return null;
  }

  /**
   * Get stored token expiry timestamp
   */
  private getStoredTokenExpiry(): number | null {
    const expiry = localStorage.getItem(this.TOKEN_EXPIRY_KEY);
    return expiry ? parseInt(expiry, 10) : null;
  }

  /**
   * Extract user role from user object
   * Constitution Rule: FE-022 (NO authorization decisions)
   * This is for UI display only, not for access control
   * 
   * @param user User object
   * @returns User role enum value
   */
  private getUserRole(user: User): UserRole | null {
    // In a real implementation, the role would come from the backend
    // For now, we'll assume it's part of the user object
    // This is just for UI display, NOT for authorization
    return (user as any).role || null;
  }
}
