import { Routes } from '@angular/router';

export const lessonsRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./lesson-list/lesson-list.component').then(m => m.LessonListComponent)
  },
  {
    path: 'new',
    loadComponent: () => import('./lesson-form/lesson-form.component').then(m => m.LessonFormComponent)
  },
  {
    path: ':id/edit',
    loadComponent: () => import('./lesson-form/lesson-form.component').then(m => m.LessonFormComponent)
  }
];
