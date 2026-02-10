import { Routes } from '@angular/router';

/**
 * Control Panel Routes
 * Admin and management functionality
 * Constitution Rule: FE-034, FE-035 (surface isolation)
 * 
 * Features will be defined in separate specs (spec 008+)
 * This is an empty shell for now
 */
export const controlPanelRoutes: Routes = [
  {
    path: '',
    children: [
      {
        path: '',
        redirectTo: 'dashboard',
        pathMatch: 'full'
      },
      {
        path: 'dashboard',
        loadComponent: () => import('./dashboard/dashboard.component').then(m => m.DashboardComponent)
      },
      {
        path: 'courses',
        loadChildren: () => import('./courses/courses.routes').then(m => m.coursesRoutes)
      },
      {
        path: 'lessons',
        loadChildren: () => import('./lessons/lessons.routes').then(m => m.lessonsRoutes)
      },
      {
        path: 'exams',
        loadChildren: () => import('./exams/exams.routes').then(m => m.examsRoutes)
      },
      {
        path: 'analytics',
        loadChildren: () => import('./analytics/analytics.routes').then(m => m.analyticsRoutes)
      }
    ]
  }
];
