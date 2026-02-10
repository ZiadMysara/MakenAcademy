import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { StateContainerComponent } from '../../../shared/components/state-container/state-container.component';

/**
 * Exam Result Component
 * Student-facing exam result display
 * Constitution Rules: FEAT-035-042 (no business logic), FEAT-021-027 (local UI state only)
 * 
 * US3: Exam Taking
 * - Displays pass/fail from backend response only
 * - Shows retry option on failure
 * - No score computation client-side
 */
@Component({
  selector: 'app-exam-result',
  standalone: true,
  imports: [CommonModule, StateContainerComponent],
  templateUrl: './exam-result.component.html',
  styleUrl: './exam-result.component.css'
})
export class ExamResultComponent implements OnInit {
  result: any = null;
  courseId: string | null = null;
  lessonId: string | null = null;
  error: string | null = null;

  constructor(
    private route: ActivatedRoute,
    private router: Router
  ) {
    // Get result from navigation state
    const navigation = this.router.getCurrentNavigation();
    if (navigation?.extras.state) {
      this.result = navigation.extras.state['result'];
    }
  }

  ngOnInit(): void {
    this.courseId = this.route.snapshot.paramMap.get('courseId');
    this.lessonId = this.route.snapshot.paramMap.get('lessonId');

    if (!this.result) {
      this.error = 'No exam result found';
    }

    if (!this.courseId || !this.lessonId) {
      this.error = 'Course ID or Lesson ID not provided';
    }
  }

  onRetry(): void {
    if (this.courseId && this.lessonId) {
      this.router.navigate(['/app/courses', this.courseId, 'lessons', this.lessonId, 'exam', 'session']);
    }
  }

  onBackToLesson(): void {
    if (this.courseId && this.lessonId) {
      this.router.navigate(['/app/courses', this.courseId, 'lessons', this.lessonId]);
    }
  }

  onBackToCourse(): void {
    if (this.courseId) {
      this.router.navigate(['/app/courses', this.courseId]);
    }
  }

  get isPassed(): boolean {
    return this.result?.passed === true;
  }

  get isFailed(): boolean {
    return this.result?.passed === false;
  }

  get isEmpty(): boolean {
    return !this.error && !this.result;
  }
}
