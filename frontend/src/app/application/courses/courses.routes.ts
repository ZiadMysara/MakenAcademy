import { Routes } from '@angular/router';

/**
 * Course Feature Routes (Student Surface)
 * 
 * Lazy-loaded routes for course browsing and detail viewing.
 * 
 * Constitution Compliance:
 * - [FEAT-005] Feature modules independently loadable (lazy loading)
 * - [FE-034] Control Panel and Application isolated at routing level
 * - Surface: /app/** (Student-facing)
 */
export const COURSES_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => 
      import('./course-list/course-list.component').then(m => m.CourseListComponent),
    title: 'Courses'
  },
  {
    path: ':id',
    loadComponent: () => 
      import('./course-detail/course-detail.component').then(m => m.CourseDetailComponent),
    title: 'Course Details'
  }
];
