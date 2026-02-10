import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { ApiService } from '../../../core/services/api.service';
import { StateContainerComponent } from '../../../shared/components/state-container/state-container.component';

/**
 * Course data model (from backend API)
 */
interface Course {
  id: string;
  name: string;
  description: string;
  isLocked: boolean;
  progress?: number;
  lessonCount?: number;
  completedLessonCount?: number;
}

/**
 * CourseListComponent (Student Surface)
 * 
 * Displays available courses for students with locked/unlocked status.
 * All UI behavior driven by backend API responses.
 * 
 * Constitution Compliance:
 * - [FEAT-001] Feature-specific UI only
 * - [FEAT-004] Backend API responses as single source of truth
 * - [FEAT-007] All UI behavior driven by backend responses
 * - [FEAT-010] Does NOT re-implement progression rules
 * - [FEAT-012] Renders locked/unlocked based solely on backend data
 * - [FEAT-019] Uses injected services (ApiService)
 * - [FEAT-021] Manages only local UI state (loading, error, data)
 * - [FEAT-028] Depends on Frontend Core services only
 * - Mobile-first responsive (Constitution §12.I)
 */
@Component({
  selector: 'app-course-list',
  standalone: true,
  imports: [CommonModule, StateContainerComponent],
  templateUrl: './course-list.component.html',
  styleUrls: ['./course-list.component.css']
})
export class CourseListComponent implements OnInit {
  private apiService = inject(ApiService);
  private router = inject(Router);

  // Local UI state only (FEAT-021)
  loading = false;
  error: string | null = null;
  courses: Course[] = [];

  ngOnInit(): void {
    this.loadCourses();
  }

  /**
   * Fetches courses from backend API
   * No client-side progression checks (FEAT-010)
   */
  loadCourses(): void {
    this.loading = true;
    this.error = null;

    this.apiService.get<Course[]>('api/courses').subscribe({
      next: (courses) => {
        this.courses = courses;
        this.loading = false;
      },
      error: (err) => {
        this.error = err.message || 'Failed to load courses';
        this.loading = false;
      }
    });
  }

  /**
   * Handles retry button click from StateContainer
   */
  onRetry(): void {
    this.loadCourses();
  }

  /**
   * Navigates to course detail page
   * Only navigates if course is not locked (UI enforcement only)
   */
  onCourseClick(course: Course): void {
    if (!course.isLocked) {
      this.router.navigate(['/app/courses', course.id]);
    }
  }

  /**
   * Checks if courses list is empty
   */
  get isEmpty(): boolean {
    return !this.loading && !this.error && this.courses.length === 0;
  }

  /**
   * Gets progress percentage for display
   */
  getProgressPercentage(course: Course): number {
    if (!course.lessonCount || course.lessonCount === 0) return 0;
    const completed = course.completedLessonCount || 0;
    return Math.round((completed / course.lessonCount) * 100);
  }
}
