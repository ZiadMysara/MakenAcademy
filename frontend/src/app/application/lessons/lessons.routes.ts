import { Routes } from '@angular/router';

/**
 * Lesson Routes
 * Student-facing lesson consumption
 * Constitution Rule: FEAT-029, FEAT-030 (feature isolation)
 * 
 * US2: Lesson Consumption
 * - Lesson viewer with video/PDF content type from backend
 * - Completion status from backend
 * - Exam entry point if backend indicates available
 */
export const LESSONS_ROUTES: Routes = [
  {
    path: ':lessonId',
    loadComponent: () => import('./lesson-viewer/lesson-viewer.component').then(m => m.LessonViewerComponent)
  }
];
