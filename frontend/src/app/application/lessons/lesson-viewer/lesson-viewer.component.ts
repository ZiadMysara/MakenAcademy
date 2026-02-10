import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { ApiService } from '../../../core/services/api.service';
import { StateContainerComponent } from '../../../shared/components/state-container/state-container.component';

/**
 * Lesson Viewer Component
 * Student-facing lesson consumption
 * Constitution Rules: FEAT-035-042 (no business logic), FEAT-021-027 (local UI state only)
 * 
 * US2: Lesson Consumption
 * - Renders video or PDF based on backend content type field
 * - Shows completion indicator from backend response
 * - Shows exam entry point if backend indicates available
 * - No client-side content type inference
 */
@Component({
  selector: 'app-lesson-viewer',
  standalone: true,
  imports: [CommonModule, StateContainerComponent],
  templateUrl: './lesson-viewer.component.html',
  styleUrl: './lesson-viewer.component.css'
})
export class LessonViewerComponent implements OnInit {
  loading = false;
  error: string | null = null;
  lesson: any = null;
  courseId: string | null = null;
  lessonId: string | null = null;

  constructor(
    private apiService: ApiService,
    private route: ActivatedRoute,
    private router: Router,
    private sanitizer: DomSanitizer
  ) {}

  ngOnInit(): void {
    this.courseId = this.route.snapshot.paramMap.get('courseId');
    this.lessonId = this.route.snapshot.paramMap.get('lessonId');

    if (!this.courseId || !this.lessonId) {
      this.error = 'Course ID or Lesson ID not provided';
      return;
    }

    this.loadLesson();
  }

  loadLesson(): void {
    if (!this.courseId || !this.lessonId) return;

    this.loading = true;
    this.error = null;

    this.apiService.get(`api/courses/${this.courseId}/lessons/${this.lessonId}`)
      .subscribe({
        next: (data) => {
          this.lesson = data;
          this.loading = false;
        },
        error: (err) => {
          this.error = err.message || 'Failed to load lesson';
          this.loading = false;
        }
      });
  }

  onRetry(): void {
    this.loadLesson();
  }

  onBackClick(): void {
    if (this.courseId) {
      this.router.navigate(['/app/courses', this.courseId]);
    }
  }

  onStartExam(): void {
    if (this.courseId && this.lessonId) {
      this.router.navigate(['/app/courses', this.courseId, 'lessons', this.lessonId, 'exam']);
    }
  }

  get isEmpty(): boolean {
    return !this.loading && !this.error && !this.lesson;
  }

  get isVideo(): boolean {
    return this.lesson?.contentType === 'video';
  }

  get isPdf(): boolean {
    return this.lesson?.contentType === 'pdf';
  }

  get isCompleted(): boolean {
    return this.lesson?.isCompleted === true;
  }

  get hasExam(): boolean {
    return this.lesson?.hasExam === true;
  }

  get isLocked(): boolean {
    return this.lesson?.isLocked === true;
  }

  getSafeUrl(url: string): SafeResourceUrl {
    return this.sanitizer.bypassSecurityTrustResourceUrl(url);
  }
}
