import { ApplicationConfig, provideBrowserGlobalErrorListeners, APP_INITIALIZER } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';

import { routes } from './app.routes';
import { TenantService } from './core/services/tenant.service';
import { ThemeService } from './core/services/theme.service';
import { tenantInterceptor } from './core/interceptors/tenant.interceptor';

/**
 * Tenant Initialization Factory
 * Resolves tenant context before application bootstrap
 * Constitution Rule: FE-018 (tenant resolution before feature modules)
 */
export function initializeTenant(tenantService: TenantService) {
  return () => tenantService.resolveTenant()
    .catch(error => {
      console.error('Tenant initialization failed:', error);
      return Promise.resolve();
    });
}

/**
 * Theme Initialization Factory
 * Loads and applies theme after tenant resolution
 * Constitution Rule: FE-028 (tenant-aware theming loaded at runtime)
 */
export function initializeTheme(themeService: ThemeService) {
  return () => themeService.loadTheme();
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(
      withInterceptors([tenantInterceptor])
    ),
    {
      provide: APP_INITIALIZER,
      useFactory: initializeTenant,
      deps: [TenantService],
      multi: true
    },
    {
      provide: APP_INITIALIZER,
      useFactory: initializeTheme,
      deps: [ThemeService],
      multi: true
    }
  ]
};
