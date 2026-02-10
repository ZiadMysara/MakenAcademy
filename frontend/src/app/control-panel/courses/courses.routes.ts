import { Routes } from '@angular/router';

/**
 * Admin Course Routes
 * Control Panel - Course Management
 * Constitution Rule: FEAT-029, FEAT-030 (feature isolation)
 * 
 * US4: Admin Curriculum Management
 * - Course list with CRUD operations
 * - Course form for create/edit
 */
export const coursesRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./course-list/course-list.component').then(m => m.CourseListComponent)
  },
  {
    path: 'new',
    loadComponent: () => import('./course-form/course-form.component').then(m => m.CourseFormComponent)
  },
  {
    path: ':id/edit',
    loadComponent: () => import('./course-form/course-form.component').then(m => m.CourseFormComponent)
  }
];
