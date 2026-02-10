import { Routes } from '@angular/router';

/**
 * Exam Routes
 * Student-facing exam taking
 * Constitution Rule: FEAT-029, FEAT-030 (feature isolation)
 * 
 * US3: Exam Taking
 * - Exam session with MCQ questions from backend
 * - Exam result with pass/fail from backend
 * - No client-side scoring or reordering
 */
export const EXAMS_ROUTES: Routes = [
  {
    path: 'session',
    loadComponent: () => import('./exam-session/exam-session.component').then(m => m.ExamSessionComponent)
  },
  {
    path: 'result',
    loadComponent: () => import('./exam-result/exam-result.component').then(m => m.ExamResultComponent)
  }
];
