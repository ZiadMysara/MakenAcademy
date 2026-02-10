import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, ActivatedRoute } from '@angular/router';
import { ApiService } from '../../../core/services/api.service';
import { StateContainerComponent } from '../../../shared/components/state-container/state-container.component';
import { DataTableComponent, DataTableColumn } from '../../../shared/components/data-table/data-table.component';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-lesson-list',
  standalone: true,
  imports: [CommonModule, StateContainerComponent, DataTableComponent, ConfirmDialogComponent],
  templateUrl: './lesson-list.component.html',
  styleUrl: './lesson-list.component.css'
})
export class LessonListComponent implements OnInit {
  loading = false;
  error: string | null = null;
  lessons: any[] = [];
  courseId: string | null = null;
  
  showDeleteDialog = false;
  lessonToDelete: any = null;

  columns: DataTableColumn[] = [
    { key: 'title', label: 'Title' },
    { key: 'type', label: 'Type' },
    { key: 'order', label: 'Order' }
  ];

  constructor(
    private apiService: ApiService,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    this.courseId = this.route.snapshot.queryParamMap.get('courseId');
    if (this.courseId) {
      this.loadLessons();
    } else {
      this.error = 'Course ID is required';
    }
  }

  loadLessons(): void {
    if (!this.courseId) return;

    this.loading = true;
    this.error = null;

    this.apiService.get(`api/courses/${this.courseId}/lessons`)
      .subscribe({
        next: (data: any) => {
          this.lessons = data;
          this.loading = false;
        },
        error: (err) => {
          this.error = err.message || 'Failed to load lessons';
          this.loading = false;
        }
      });
  }

  onRetry(): void {
    this.loadLessons();
  }

  onRowAction(event: { action: string; row: any }): void {
    if (event.action === 'edit') {
      this.router.navigate(['/control-panel/lessons', event.row.id, 'edit'], {
        queryParams: { courseId: this.courseId }
      });
    } else if (event.action === 'delete') {
      this.lessonToDelete = event.row;
      this.showDeleteDialog = true;
    }
  }

  onCreateNew(): void {
    this.router.navigate(['/control-panel/lessons/new'], {
      queryParams: { courseId: this.courseId }
    });
  }

  onConfirmDelete(): void {
    if (!this.lessonToDelete || !this.courseId) return;

    this.apiService.delete(`api/courses/${this.courseId}/lessons/${this.lessonToDelete.id}`)
      .subscribe({
        next: () => {
          this.showDeleteDialog = false;
          this.lessonToDelete = null;
          this.loadLessons();
        },
        error: (err) => {
          this.error = err.message || 'Failed to delete lesson';
          this.showDeleteDialog = false;
          this.lessonToDelete = null;
        }
      });
  }

  onCancelDelete(): void {
    this.showDeleteDialog = false;
    this.lessonToDelete = null;
  }

  get isEmpty(): boolean {
    return !this.loading && !this.error && this.lessons.length === 0;
  }
}
