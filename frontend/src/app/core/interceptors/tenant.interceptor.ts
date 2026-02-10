import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { TenantService } from '../services/tenant.service';

/**
 * Tenant Interceptor
 * Injects tenant ID header into all outgoing API requests
 * Constitution Rules: FE-014, FE-015, FE-016
 * 
 * Key Responsibilities:
 * - Add X-Tenant-ID header to all API requests
 * - Ensure tenant context is available before making requests
 * - Skip header injection for non-API requests (external URLs)
 * 
 * Header Format:
 * X-Tenant-ID: <tenant-uuid>
 * 
 * Usage:
 * Automatically applied to all HTTP requests via app.config.ts:
 * ```
 * provideHttpClient(
 *   withInterceptors([tenantInterceptor])
 * )
 * ```
 */
export const tenantInterceptor: HttpInterceptorFn = (req, next) => {
  const tenantService = inject(TenantService);
  
  // Get current tenant context
  const tenant = tenantService.currentTenant;
  
  // Only inject header if tenant is resolved and request is to our API
  if (tenant && req.url.includes('/api/')) {
    // Clone request and add tenant header
    const clonedRequest = req.clone({
      setHeaders: {
        'X-Tenant-ID': tenant.id
      }
    });
    
    return next(clonedRequest);
  }
  
  // Pass through without modification
  return next(req);
};
