import { Routes } from '@angular/router';

/**
 * Application Routes
 * Student-facing functionality
 * Constitution Rule: FE-034, FE-035, FE-040 (surface isolation, no admin leakage)
 * 
 * Features will be defined in separate specs (spec 008+)
 * This is an empty shell for now
 */
export const applicationRoutes: Routes = [
  {
    path: '',
    children: [
      {
        path: '',
        redirectTo: 'home',
        pathMatch: 'full'
      },
      {
        path: 'home',
        loadComponent: () => import('./home/home.component').then(m => m.HomeComponent)
      },
      {
        path: 'courses',
        loadChildren: () => import('./courses/courses.routes').then(m => m.COURSES_ROUTES)
      },
      {
        path: 'courses/:courseId/lessons',
        loadChildren: () => import('./lessons/lessons.routes').then(m => m.LESSONS_ROUTES)
      },
      {
        path: 'courses/:courseId/lessons/:lessonId/exam',
        loadChildren: () => import('./exams/exams.routes').then(m => m.EXAMS_ROUTES)
      }
      // Additional routes will be added by feature specs
    ]
  }
];
