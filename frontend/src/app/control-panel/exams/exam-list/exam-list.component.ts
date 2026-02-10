import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { ApiService } from '../../../core/services/api.service';
import { StateContainerComponent } from '../../../shared/components/state-container/state-container.component';
import { DataTableComponent, DataTableColumn } from '../../../shared/components/data-table/data-table.component';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-exam-list',
  standalone: true,
  imports: [CommonModule, StateContainerComponent, DataTableComponent, ConfirmDialogComponent],
  templateUrl: './exam-list.component.html',
  styleUrl: './exam-list.component.css'
})
export class ExamListComponent implements OnInit {
  loading = false;
  error: string | null = null;
  exams: any[] = [];
  
  showDeleteDialog = false;
  examToDelete: any = null;

  columns: DataTableColumn[] = [
    { key: 'title', label: 'Title' },
    { key: 'questionCount', label: 'Question Count' },
    { key: 'passThreshold', label: 'Pass Threshold' }
  ];

  constructor(
    private apiService: ApiService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadExams();
  }

  loadExams(): void {
    this.loading = true;
    this.error = null;

    this.apiService.get('api/exams')
      .subscribe({
        next: (data: any) => {
          this.exams = data;
          this.loading = false;
        },
        error: (err) => {
          this.error = err.message || 'Failed to load exams';
          this.loading = false;
        }
      });
  }

  onRetry(): void {
    this.loadExams();
  }

  onRowAction(event: { action: string; row: any }): void {
    if (event.action === 'edit') {
      this.router.navigate(['/control-panel/exams', event.row.id, 'edit']);
    } else if (event.action === 'delete') {
      this.examToDelete = event.row;
      this.showDeleteDialog = true;
    }
  }

  onCreateNew(): void {
    this.router.navigate(['/control-panel/exams/new']);
  }

  onConfirmDelete(): void {
    if (!this.examToDelete) return;

    this.apiService.delete(`api/exams/${this.examToDelete.id}`)
      .subscribe({
        next: () => {
          this.showDeleteDialog = false;
          this.examToDelete = null;
          this.loadExams();
        },
        error: (err) => {
          this.error = err.message || 'Failed to delete exam';
          this.showDeleteDialog = false;
          this.examToDelete = null;
        }
      });
  }

  onCancelDelete(): void {
    this.showDeleteDialog = false;
    this.examToDelete = null;
  }

  get isEmpty(): boolean {
    return !this.loading && !this.error && this.exams.length === 0;
  }
}
