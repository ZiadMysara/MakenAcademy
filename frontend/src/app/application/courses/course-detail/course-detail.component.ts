import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { ApiService } from '../../../core/services/api.service';
import { StateContainerComponent } from '../../../shared/components/state-container/state-container.component';

/**
 * Lesson data model (from backend API)
 */
interface Lesson {
  id: string;
  title: string;
  description: string;
  order: number;
  isLocked: boolean;
  isCompleted: boolean;
  contentType: 'video' | 'pdf';
  duration?: string;
}

/**
 * Course detail data model (from backend API)
 */
interface CourseDetail {
  id: string;
  name: string;
  description: string;
  isLocked: boolean;
  progress?: number;
  lessons: Lesson[];
}

/**
 * CourseDetailComponent (Student Surface)
 * 
 * Displays course overview with lesson list.
 * Lesson locked/unlocked status from backend data only.
 * 
 * Constitution Compliance:
 * - [FEAT-001] Feature-specific UI only
 * - [FEAT-004] Backend API responses as single source of truth
 * - [FEAT-007] All UI behavior driven by backend responses
 * - [FEAT-010] Does NOT re-implement progression rules
 * - [FEAT-012] Renders locked/unlocked based solely on backend data
 * - [FEAT-019] Uses injected services (ApiService)
 * - [FEAT-021] Manages only local UI state (loading, error, data)
 * - [FEAT-042] Does NOT implement lesson unlock logic
 * - Mobile-first responsive (Constitution §12.I)
 */
@Component({
  selector: 'app-course-detail',
  standalone: true,
  imports: [CommonModule, StateContainerComponent],
  templateUrl: './course-detail.component.html',
  styleUrls: ['./course-detail.component.css']
})
export class CourseDetailComponent implements OnInit {
  private apiService = inject(ApiService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  // Local UI state only (FEAT-021)
  loading = false;
  error: string | null = null;
  course: CourseDetail | null = null;

  ngOnInit(): void {
    const courseId = this.route.snapshot.paramMap.get('id');
    if (courseId) {
      this.loadCourseDetail(courseId);
    } else {
      this.error = 'Course ID not provided';
    }
  }

  /**
   * Fetches course detail from backend API
   * No client-side progression checks (FEAT-010, FEAT-042)
   */
  loadCourseDetail(courseId: string): void {
    this.loading = true;
    this.error = null;

    this.apiService.get<CourseDetail>(`api/courses/${courseId}`).subscribe({
      next: (course) => {
        this.course = course;
        this.loading = false;
      },
      error: (err) => {
        this.error = err.message || 'Failed to load course details';
        this.loading = false;
      }
    });
  }

  /**
   * Handles retry button click from StateContainer
   */
  onRetry(): void {
    const courseId = this.route.snapshot.paramMap.get('id');
    if (courseId) {
      this.loadCourseDetail(courseId);
    }
  }

  /**
   * Navigates to lesson viewer
   * Only navigates if lesson is not locked (UI enforcement only)
   */
  onLessonClick(lesson: Lesson): void {
    if (!lesson.isLocked && this.course) {
      this.router.navigate(['/app/courses', this.course.id, 'lessons', lesson.id]);
    }
  }

  /**
   * Navigates back to course list
   */
  onBackClick(): void {
    this.router.navigate(['/app/courses']);
  }

  /**
   * Checks if course data is empty
   */
  get isEmpty(): boolean {
    return !this.loading && !this.error && this.course === null;
  }

  /**
   * Gets lesson icon based on content type
   */
  getLessonIcon(lesson: Lesson): string {
    if (lesson.isLocked) return '🔒';
    if (lesson.isCompleted) return '✓';
    return lesson.contentType === 'video' ? '🎥' : '📄';
  }

  /**
   * Gets lesson status text
   */
  getLessonStatus(lesson: Lesson): string {
    if (lesson.isLocked) return 'Locked';
    if (lesson.isCompleted) return 'Completed';
    return 'Available';
  }
}
