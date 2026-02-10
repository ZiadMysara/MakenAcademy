import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';

/**
 * Authentication Interceptor
 * Injects JWT token into requests and handles 401 errors
 * Constitution Rules: FE-020, FE-025 (transparent token refresh)
 * 
 * Key Responsibilities:
 * - Add Authorization header with JWT token to API requests
 * - Handle 401 Unauthorized responses
 * - Attempt token refresh on 401
 * - Logout and redirect to login if refresh fails
 * 
 * Token Refresh Flow:
 * 1. Request fails with 401
 * 2. Attempt to refresh token
 * 3. If refresh succeeds, retry original request
 * 4. If refresh fails, logout and redirect to login
 * 
 * Usage:
 * Automatically applied to all HTTP requests via app.config.ts:
 * ```
 * provideHttpClient(
 *   withInterceptors([authInterceptor])
 * )
 * ```
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  
  // Get current auth state
  const authState = authService.currentAuthState;
  
  // Only inject token if user is authenticated and request is to our API
  let clonedRequest = req;
  if (authState.isAuthenticated && authState.token && req.url.includes('/api/')) {
    // Clone request and add Authorization header
    clonedRequest = req.clone({
      setHeaders: {
        'Authorization': `Bearer ${authState.token}`
      }
    });
  }
  
  // Handle the request and catch 401 errors
  return next(clonedRequest).pipe(
    catchError((error: HttpErrorResponse) => {
      // Handle 401 Unauthorized
      if (error.status === 401 && authState.isAuthenticated) {
        // Attempt token refresh
        return authService.refreshToken().pipe(
          switchMap(() => {
            // Refresh succeeded - retry original request with new token
            const newAuthState = authService.currentAuthState;
            const retryRequest = req.clone({
              setHeaders: {
                'Authorization': `Bearer ${newAuthState.token}`
              }
            });
            return next(retryRequest);
          }),
          catchError((refreshError) => {
            // Refresh failed - logout and redirect to login
            console.error('Token refresh failed. Logging out.');
            authService.logout();
            router.navigate(['/login']);
            return throwError(() => refreshError);
          })
        );
      }
      
      // Not a 401 or not authenticated - pass error through
      return throwError(() => error);
    })
  );
};
