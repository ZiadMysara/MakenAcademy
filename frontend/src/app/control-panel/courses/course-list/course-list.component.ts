import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { ApiService } from '../../../core/services/api.service';
import { StateContainerComponent } from '../../../shared/components/state-container/state-container.component';
import { DataTableComponent, DataTableColumn } from '../../../shared/components/data-table/data-table.component';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-course-list',
  standalone: true,
  imports: [CommonModule, StateContainerComponent, DataTableComponent, ConfirmDialogComponent],
  templateUrl: './course-list.component.html',
  styleUrl: './course-list.component.css'
})
export class CourseListComponent implements OnInit {
  loading = false;
  error: string | null = null;
  courses: any[] = [];
  
  showDeleteDialog = false;
  courseToDelete: any = null;

  columns: DataTableColumn[] = [
    { key: 'name', label: 'Name' },
    { key: 'status', label: 'Status' },
    { key: 'lessonCount', label: 'Lesson Count' }
  ];

  constructor(
    private apiService: ApiService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadCourses();
  }

  loadCourses(): void {
    this.loading = true;
    this.error = null;

    this.apiService.get('api/courses')
      .subscribe({
        next: (data: any) => {
          this.courses = data;
          this.loading = false;
        },
        error: (err) => {
          this.error = err.message || 'Failed to load courses';
          this.loading = false;
        }
      });
  }

  onRetry(): void {
    this.loadCourses();
  }

  onRowAction(event: { action: string; row: any }): void {
    if (event.action === 'edit') {
      this.router.navigate(['/control-panel/courses', event.row.id, 'edit']);
    } else if (event.action === 'delete') {
      this.courseToDelete = event.row;
      this.showDeleteDialog = true;
    }
  }

  onCreateNew(): void {
    this.router.navigate(['/control-panel/courses/new']);
  }

  onConfirmDelete(): void {
    if (!this.courseToDelete) return;

    this.apiService.delete(`api/courses/${this.courseToDelete.id}`)
      .subscribe({
        next: () => {
          this.showDeleteDialog = false;
          this.courseToDelete = null;
          this.loadCourses();
        },
        error: (err) => {
          this.error = err.message || 'Failed to delete course';
          this.showDeleteDialog = false;
          this.courseToDelete = null;
        }
      });
  }

  onCancelDelete(): void {
    this.showDeleteDialog = false;
    this.courseToDelete = null;
  }

  get isEmpty(): boolean {
    return !this.loading && !this.error && this.courses.length === 0;
  }
}
