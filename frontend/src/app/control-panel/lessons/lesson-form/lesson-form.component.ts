import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ApiService } from '../../../core/services/api.service';
import { FormShellComponent } from '../../../shared/components/form-shell/form-shell.component';

@Component({
  selector: 'app-lesson-form',
  standalone: true,
  imports: [CommonModule, FormsModule, FormShellComponent],
  templateUrl: './lesson-form.component.html',
  styleUrl: './lesson-form.component.css'
})
export class LessonFormComponent implements OnInit {
  isEditMode = false;
  lessonId: string | null = null;
  courseId: string | null = null;
  
  lesson = {
    title: '',
    description: '',
    contentType: 'video',
    contentUrl: '',
    order: 1
  };

  pristine = true;
  submitting = false;
  backendErrors: any = null;

  constructor(
    private apiService: ApiService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.lessonId = this.route.snapshot.paramMap.get('id');
    this.courseId = this.route.snapshot.queryParamMap.get('courseId');
    this.isEditMode = !!this.lessonId;

    if (this.isEditMode && this.lessonId && this.courseId) {
      this.loadLesson();
    }
  }

  loadLesson(): void {
    if (!this.lessonId || !this.courseId) return;

    this.apiService.get(`api/courses/${this.courseId}/lessons/${this.lessonId}`)
      .subscribe({
        next: (data: any) => {
          this.lesson = {
            title: data.title || '',
            description: data.description || '',
            contentType: data.contentType || 'video',
            contentUrl: data.contentUrl || '',
            order: data.order || 1
          };
        },
        error: (err) => {
          this.backendErrors = { general: err.message || 'Failed to load lesson' };
        }
      });
  }

  onFieldChange(): void {
    this.pristine = false;
  }

  onSubmit(): void {
    if (this.submitting || !this.courseId) return;

    this.submitting = true;
    this.backendErrors = null;

    const request = this.isEditMode
      ? this.apiService.put(`api/courses/${this.courseId}/lessons/${this.lessonId}`, this.lesson)
      : this.apiService.post(`api/courses/${this.courseId}/lessons`, this.lesson);

    request.subscribe({
      next: () => {
        this.router.navigate(['/control-panel/lessons'], { queryParams: { courseId: this.courseId } });
      },
      error: (err) => {
        this.backendErrors = err.error || { general: err.message || 'Failed to save lesson' };
        this.submitting = false;
      }
    });
  }

  onCancel(): void {
    this.router.navigate(['/control-panel/lessons'], { queryParams: { courseId: this.courseId } });
  }

  get isDirty(): boolean {
    return !this.pristine;
  }
}
