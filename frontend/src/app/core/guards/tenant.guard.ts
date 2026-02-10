import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { TenantService } from '../services/tenant.service';

/**
 * Tenant Guard
 * Blocks navigation if tenant context has not been resolved
 * Constitution Rules: FE-014, FE-016, FE-018
 * 
 * Key Responsibilities:
 * - Check if tenant context is resolved before allowing navigation
 * - Redirect to error page if tenant is not resolved
 * - Ensure tenant resolution occurs before feature modules initialize
 * 
 * Usage:
 * Apply to routes that require tenant context:
 * ```
 * {
 *   path: 'app',
 *   canActivate: [tenantGuard],
 *   loadChildren: () => import('./application/application.routes')
 * }
 * ```
 */
export const tenantGuard: CanActivateFn = (route, state) => {
  const tenantService = inject(TenantService);
  const router = inject(Router);

  // Check if tenant has been resolved
  if (tenantService.isTenantResolved) {
    return true;
  }

  // Tenant not resolved - redirect to error page
  console.error('Tenant not resolved. Redirecting to error page.');
  router.navigate(['/error'], { 
    queryParams: { 
      reason: 'tenant-not-resolved',
      returnUrl: state.url 
    } 
  });
  
  return false;
};
