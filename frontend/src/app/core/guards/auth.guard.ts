import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

/**
 * Authentication Guard
 * Redirects to login page if user is not authenticated
 * Constitution Rules: FE-020, FE-021, FE-022
 * 
 * Key Responsibilities:
 * - Check if user is authenticated before allowing navigation
 * - Redirect to login page if not authenticated
 * - Store return URL for post-login redirect
 * - NO authorization decisions (only authentication check)
 * 
 * Usage:
 * Apply to routes that require authentication:
 * ```
 * {
 *   path: 'app',
 *   canActivate: [authGuard],
 *   loadChildren: () => import('./application/application.routes')
 * }
 * ```
 */
export const authGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  // Check if user is authenticated
  if (authService.isAuthenticated) {
    return true;
  }

  // Not authenticated - redirect to login
  console.warn('User not authenticated. Redirecting to login.');
  router.navigate(['/login'], { 
    queryParams: { 
      returnUrl: state.url 
    } 
  });
  
  return false;
};
