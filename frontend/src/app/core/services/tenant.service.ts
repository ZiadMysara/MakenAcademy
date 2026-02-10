import { Injectable, inject } from '@angular/core';
import { BehaviorSubject, Observable, throwError } from 'rxjs';
import { catchError, tap } from 'rxjs/operators';
import { TenantContext } from '../models/tenant.model';
import { ApiService } from './api.service';

/**
 * Tenant Service
 * Resolves tenant context from subdomain at application startup
 * Constitution Rules: FE-014, FE-015, FE-016, FE-017, FE-018, FE-019
 * 
 * Key Responsibilities:
 * - Extract subdomain from window.location.hostname
 * - Call backend API to resolve tenant configuration
 * - Store immutable tenant context (Simplicity Rule: BehaviorSubject)
 * - Expose readonly Observable for consumption
 * - Show static error page if tenant cannot be resolved
 */
@Injectable({
  providedIn: 'root'
})
export class TenantService {
  private readonly apiService = inject(ApiService);
  
  // Immutable tenant context - set once at startup
  private readonly tenantSubject = new BehaviorSubject<TenantContext | null>(null);
  
  // Readonly Observable for external consumption
  public readonly tenant$: Observable<TenantContext | null> = this.tenantSubject.asObservable();
  
  // Flag to track if tenant has been resolved
  private isResolved = false;

  /**
   * Get current tenant context (synchronous)
   * Returns null if tenant not yet resolved
   */
  get currentTenant(): TenantContext | null {
    return this.tenantSubject.value;
  }

  /**
   * Check if tenant has been successfully resolved
   */
  get isTenantResolved(): boolean {
    return this.isResolved && this.tenantSubject.value !== null;
  }

  /**
   * Resolve tenant from subdomain
   * This should be called once at application startup (APP_INITIALIZER)
   * Constitution Rule: FE-018 (must occur before feature modules initialize)
   * 
   * @returns Promise that resolves when tenant is loaded or rejects on error
   */
  resolveTenant(): Promise<TenantContext> {
    return new Promise((resolve, reject) => {
      // Extract subdomain from hostname
      const subdomain = this.extractSubdomain();
      
      if (!subdomain) {
        const error = new Error('Unable to determine tenant from URL');
        reject(error);
        return;
      }

      // Call backend API to resolve tenant
      this.apiService
        .get<TenantContext>(`api/tenants/resolve?subdomain=${subdomain}`)
        .pipe(
          tap(tenant => {
            // Store immutable tenant context
            this.tenantSubject.next(tenant);
            this.isResolved = true;
          }),
          catchError(error => {
            console.error('Failed to resolve tenant:', error);
            this.isResolved = false;
            return throwError(() => new Error('Failed to resolve tenant. Please check your URL.'));
          })
        )
        .subscribe({
          next: (tenant) => resolve(tenant),
          error: (error) => reject(error)
        });
    });
  }

  /**
   * Extract subdomain from window.location.hostname
   * Constitution Rule: FE-014 (subdomain-based tenant resolution)
   * 
   * Examples:
   * - academy.maken.app → "academy"
   * - localhost:4200 → "localhost" (for development)
   * - maken.app → null (root domain, no tenant)
   * 
   * @returns Subdomain string or null if not found
   */
  private extractSubdomain(): string | null {
    const hostname = this.getHostname();
    
    // Development: localhost or 127.0.0.1
    if (hostname === 'localhost' || hostname === '127.0.0.1') {
      // For local development, you can use a query parameter or default tenant
      // For now, return 'localhost' as a special case
      return 'localhost';
    }

    // Production: Extract subdomain from hostname
    const parts = hostname.split('.');
    
    // Need at least 3 parts for subdomain (e.g., academy.maken.app)
    if (parts.length < 3) {
      return null; // Root domain or invalid
    }

    // Return the first part as subdomain
    return parts[0];
  }

  /**
   * Get hostname from window.location
   * Protected for testing purposes
   */
  protected getHostname(): string {
    return window.location.hostname;
  }

  /**
   * Clear tenant context (for testing purposes only)
   * Not used in production - tenant is immutable after resolution
   */
  clearTenant(): void {
    this.tenantSubject.next(null);
    this.isResolved = false;
  }
}
