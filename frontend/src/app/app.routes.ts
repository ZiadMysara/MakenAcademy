import { Routes } from '@angular/router';
import { tenantGuard } from './core/guards/tenant.guard';
import { authGuard } from './core/guards/auth.guard';

/**
 * Top-Level Application Routes
 * Constitution Rules: FE-034, FE-035 (surface isolation)
 * 
 * Surface Separation:
 * - /admin/** → Control Panel (lazy-loaded)
 * - /app/** → Application (lazy-loaded)
 * - Both surfaces protected by TenantGuard and AuthGuard
 * - Shared services are singleton across both surfaces
 */
export const routes: Routes = [
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
    path: '',
    redirectTo: '/app',
    pathMatch: 'full'
  },
  {
    path: '**',
    redirectTo: '/error'
  }
];
