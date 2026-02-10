import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ApiService } from '../../../core/services/api.service';
import { FormShellComponent } from '../../../shared/components/form-shell/form-shell.component';

@Component({
  selector: 'app-course-form',
  standalone: true,
  imports: [CommonModule, FormsModule, FormShellComponent],
  templateUrl: './course-form.component.html',
  styleUrl: './course-form.component.css'
})
export class CourseFormComponent implements OnInit {
  isEditMode = false;
  courseId: string | null = null;
  
  course = {
    name: '',
    description: '',
    status: 'draft'
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
    this.courseId = this.route.snapshot.paramMap.get('id');
    this.isEditMode = !!this.courseId;

    if (this.isEditMode && this.courseId) {
      this.loadCourse();
    }
  }

  loadCourse(): void {
    if (!this.courseId) return;

    this.apiService.get(`api/courses/${this.courseId}`)
      .subscribe({
        next: (data: any) => {
          this.course = {
            name: data.name || '',
            description: data.description || '',
            status: data.status || 'draft'
          };
        },
        error: (err) => {
          this.backendErrors = { general: err.message || 'Failed to load course' };
        }
      });
  }

  onFieldChange(): void {
    this.pristine = false;
  }

  onSubmit(): void {
    if (this.submitting) return;

    this.submitting = true;
    this.backendErrors = null;

    const request = this.isEditMode
      ? this.apiService.put(`api/courses/${this.courseId}`, this.course)
      : this.apiService.post('api/courses', this.course);

    request.subscribe({
      next: () => {
        this.router.navigate(['/control-panel/courses']);
      },
      error: (err) => {
        this.backendErrors = err.error || { general: err.message || 'Failed to save course' };
        this.submitting = false;
      }
    });
  }

  onCancel(): void {
    this.router.navigate(['/control-panel/courses']);
  }

  get isDirty(): boolean {
    return !this.pristine;
  }
}
