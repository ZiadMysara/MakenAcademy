import { Injectable, inject } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { TenantTheme } from '../models/tenant.model';
import { TenantService } from './tenant.service';

/**
 * Theme Service
 * Loads and applies tenant-specific theming at runtime
 * Constitution Rules: FE-027, FE-028, FE-029, FE-032, FE-033
 * 
 * Key Responsibilities:
 * - Load theme configuration from tenant context (already resolved)
 * - Apply CSS custom properties to document.documentElement
 * - Expose theme state as readonly Observable
 * - NO hardcoded tenant-specific styles
 * - Support default theme fallback
 * 
 * CSS Variables Applied:
 * - --color-primary: Primary brand color
 * - --color-secondary: Secondary brand color
 * - --logo-url: Tenant logo URL
 * 
 * Usage:
 * Called during app initialization after tenant resolution
 */
@Injectable({
  providedIn: 'root'
})
export class ThemeService {
  private readonly tenantService = inject(TenantService);
  
  // Theme state - BehaviorSubject for Simplicity Rule
  private readonly themeSubject = new BehaviorSubject<TenantTheme | null>(null);
  
  // Readonly Observable for external consumption
  public readonly theme$: Observable<TenantTheme | null> = this.themeSubject.asObservable();
  
  // Default theme fallback
  private readonly defaultTheme: TenantTheme = {
    primaryColor: '#1E40AF', // Blue
    secondaryColor: '#64748B', // Slate
    logoUrl: undefined
  };

  /**
   * Get current theme (synchronous)
   */
  get currentTheme(): TenantTheme | null {
    return this.themeSubject.value;
  }

  /**
   * Load and apply theme from tenant context
   * Constitution Rule: FE-028 (tenant-aware theming loaded at runtime)
   * 
   * This should be called after tenant resolution in APP_INITIALIZER
   * 
   * @returns Promise that resolves when theme is applied
   */
  loadTheme(): Promise<void> {
    return new Promise((resolve) => {
      const tenant = this.tenantService.currentTenant;
      
      // Check if tenant has a valid theme with at least one property
      const hasValidTheme = tenant && tenant.theme && (
        tenant.theme.primaryColor || 
        tenant.theme.secondaryColor || 
        tenant.theme.logoUrl
      );
      
      if (hasValidTheme) {
        // Use tenant theme (merge with defaults for missing properties)
        const mergedTheme = {
          ...this.defaultTheme,
          ...tenant.theme
        };
        this.applyTheme(mergedTheme);
        this.themeSubject.next(mergedTheme);
      } else {
        // Use default theme fallback
        console.warn('No tenant theme found. Applying default theme.');
        this.applyTheme(this.defaultTheme);
        this.themeSubject.next(this.defaultTheme);
      }
      
      resolve();
    });
  }

  /**
   * Apply theme by setting CSS custom properties
   * Constitution Rule: FE-028 (apply CSS custom properties to document.documentElement)
   * 
   * @param theme Theme configuration to apply
   */
  private applyTheme(theme: TenantTheme): void {
    const root = document.documentElement;
    
    // Apply primary color
    if (theme.primaryColor) {
      root.style.setProperty('--color-primary', theme.primaryColor);
      // Generate lighter and darker variants
      root.style.setProperty('--color-primary-light', this.lightenColor(theme.primaryColor, 20));
      root.style.setProperty('--color-primary-dark', this.darkenColor(theme.primaryColor, 20));
    }
    
    // Apply secondary color
    if (theme.secondaryColor) {
      root.style.setProperty('--color-secondary', theme.secondaryColor);
      root.style.setProperty('--color-secondary-light', this.lightenColor(theme.secondaryColor, 20));
      root.style.setProperty('--color-secondary-dark', this.darkenColor(theme.secondaryColor, 20));
    }
    
    // Apply logo URL (for use in CSS background-image)
    if (theme.logoUrl) {
      root.style.setProperty('--logo-url', `url(${theme.logoUrl})`);
    }
  }

  /**
   * Reset theme to default
   * Useful for testing or tenant switching
   */
  resetTheme(): void {
    this.applyTheme(this.defaultTheme);
    this.themeSubject.next(this.defaultTheme);
  }

  /**
   * Lighten a hex color by a percentage
   * 
   * @param color Hex color (e.g., "#1E40AF")
   * @param percent Percentage to lighten (0-100)
   * @returns Lightened hex color
   */
  private lightenColor(color: string, percent: number): string {
    const num = parseInt(color.replace('#', ''), 16);
    const amt = Math.round(2.55 * percent);
    const R = (num >> 16) + amt;
    const G = (num >> 8 & 0x00FF) + amt;
    const B = (num & 0x0000FF) + amt;
    
    return '#' + (
      0x1000000 +
      (R < 255 ? (R < 1 ? 0 : R) : 255) * 0x10000 +
      (G < 255 ? (G < 1 ? 0 : G) : 255) * 0x100 +
      (B < 255 ? (B < 1 ? 0 : B) : 255)
    ).toString(16).slice(1).toUpperCase();
  }

  /**
   * Darken a hex color by a percentage
   * 
   * @param color Hex color (e.g., "#1E40AF")
   * @param percent Percentage to darken (0-100)
   * @returns Darkened hex color
   */
  private darkenColor(color: string, percent: number): string {
    const num = parseInt(color.replace('#', ''), 16);
    const amt = Math.round(2.55 * percent);
    const R = (num >> 16) - amt;
    const G = (num >> 8 & 0x00FF) - amt;
    const B = (num & 0x0000FF) - amt;
    
    return '#' + (
      0x1000000 +
      (R > 0 ? R : 0) * 0x10000 +
      (G > 0 ? G : 0) * 0x100 +
      (B > 0 ? B : 0)
    ).toString(16).slice(1).toUpperCase();
  }
}
