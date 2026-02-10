import { Routes } from '@angular/router';

/**
 * Analytics Routes
 * Control Panel - Analytics Dashboard
 * Constitution Rule: FEAT-029, FEAT-030 (feature isolation)
 * 
 * US5: Analytics Dashboard
 * - Simple descriptive analytics (enrollment, completion)
 * - All data from backend, no client-side computation
 */
export const analyticsRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./analytics-dashboard/analytics-dashboard.component').then(m => m.AnalyticsDashboardComponent)
  }
];
