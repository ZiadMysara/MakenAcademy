import { Routes } from '@angular/router';
import { tenantGuard } from './core/guards/tenant.guard';
import { authGuard } from './core/guards/auth.guard';

/**
 * Top-Level Application Routes
 * Constitution Rules: FE-034, FE-035 (surface isolation)
 * 
 * Surface Separation:
 * - / → Public Landing Page (no guards)
 * - /admin/** → Control Panel (lazy-loaded)
 * - /app/** → Application (lazy-loaded)
 * - Admin and App surfaces protected by TenantGuard and AuthGuard
 * - Shared services are singleton across all surfaces
 */
export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./public/landing-page/landing-page').then(m => m.LandingPage)
  },
  {
    path: 'error',
    loadComponent: () => import('./shared/components/error-page/error-page.component').then(m => m.ErrorPageComponent)
  },
  {
    path: 'admin',
    canActivate: [tenantGuard, authGuard],
    loadChildren: () => import('./control-panel/control-panel.routes').then(m => m.controlPanelRoutes)
  },
  {
    path: 'app',
    canActivate: [tenantGuard, authGuard],
    loadChildren: () => import('./application/application.routes').then(m => m.applicationRoutes)
  },
  {
    path: '**',
    redirectTo: '/error'
  }
];
