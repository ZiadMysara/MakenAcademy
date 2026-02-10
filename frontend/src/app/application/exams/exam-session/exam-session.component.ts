import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../../core/services/api.service';
import { StateContainerComponent } from '../../../shared/components/state-container/state-container.component';

/**
 * Exam Session Component
 * Student-facing exam taking
 * Constitution Rules: FEAT-035-042 (no business logic), FEAT-021-027 (local UI state only)
 * 
 * US3: Exam Taking
 * - Renders MCQ questions in order received from backend (no reordering)
 * - Tracks selected answers as local UI state only
 * - Submits answers to backend
 * - No client-side scoring
 */
@Component({
  selector: 'app-exam-session',
  standalone: true,
  imports: [CommonModule, FormsModule, StateContainerComponent],
  templateUrl: './exam-session.component.html',
  styleUrl: './exam-session.component.css'
})
export class ExamSessionComponent implements OnInit {
  loading = false;
  error: string | null = null;
  exam: any = null;
  courseId: string | null = null;
  lessonId: string | null = null;
  
  // Local UI state: selected answers (question ID -> choice ID)
  selectedAnswers: Map<string, string> = new Map();
  submitting = false;

  constructor(
    private apiService: ApiService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.courseId = this.route.snapshot.paramMap.get('courseId');
    this.lessonId = this.route.snapshot.paramMap.get('lessonId');

    if (!this.courseId || !this.lessonId) {
      this.error = 'Course ID or Lesson ID not provided';
      return;
    }

    this.loadExam();
  }

  loadExam(): void {
    if (!this.courseId || !this.lessonId) return;

    this.loading = true;
    this.error = null;

    this.apiService.get(`api/courses/${this.courseId}/lessons/${this.lessonId}/exam`)
      .subscribe({
        next: (data) => {
          this.exam = data;
          this.loading = false;
        },
        error: (err) => {
          this.error = err.message || 'Failed to load exam';
          this.loading = false;
        }
      });
  }

  onRetry(): void {
    this.loadExam();
  }

  onAnswerSelect(questionId: string, choiceId: string): void {
    this.selectedAnswers.set(questionId, choiceId);
  }

  isAnswerSelected(questionId: string, choiceId: string): boolean {
    return this.selectedAnswers.get(questionId) === choiceId;
  }

  canSubmit(): boolean {
    if (!this.exam || !this.exam.questions) return false;
    
    // Check if all questions have been answered
    return this.exam.questions.every((q: any) => this.selectedAnswers.has(q.id));
  }

  onSubmit(): void {
    if (!this.canSubmit() || this.submitting) return;

    this.submitting = true;
    this.error = null;

    // Convert Map to object for API submission
    const answers: any = {};
    this.selectedAnswers.forEach((choiceId, questionId) => {
      answers[questionId] = choiceId;
    });

    const payload = {
      examId: this.exam.id,
      answers: answers
    };

    this.apiService.post(`api/courses/${this.courseId}/lessons/${this.lessonId}/exam/submit`, payload)
      .subscribe({
        next: (result) => {
          // Navigate to result page with result data
          this.router.navigate(
            ['/app/courses', this.courseId, 'lessons', this.lessonId, 'exam', 'result'],
            { state: { result } }
          );
        },
        error: (err) => {
          this.error = err.message || 'Failed to submit exam';
          this.submitting = false;
        }
      });
  }

  onCancel(): void {
    if (this.courseId && this.lessonId) {
      this.router.navigate(['/app/courses', this.courseId, 'lessons', this.lessonId]);
    }
  }

  get isEmpty(): boolean {
    return !this.loading && !this.error && !this.exam;
  }
}
