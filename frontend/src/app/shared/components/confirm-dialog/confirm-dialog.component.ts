import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';

/**
 * ConfirmDialogComponent
 * 
 * Simple confirmation modal for destructive actions (e.g., soft delete).
 * Displays a message and emits confirm/cancel events.
 * 
 * Constitution Compliance:
 * - [FEAT-014] Reusable across both surfaces
 * - [FEAT-018] Data via inputs, events via outputs
 * - Mobile-first responsive (Constitution §12.I)
 * - Best-effort accessibility (FEAT-NFR-007)
 * 
 * Usage:
 * <app-confirm-dialog 
 *   *ngIf="showDialog"
 *   [message]="'Are you sure you want to delete this course?'"
 *   [title]="'Confirm Delete'"
 *   [confirmLabel]="'Delete'"
 *   [cancelLabel]="'Cancel'"
 *   (confirm)="onConfirm()"
 *   (cancel)="onCancel()">
 * </app-confirm-dialog>
 */
@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './confirm-dialog.component.html',
  styleUrls: ['./confirm-dialog.component.css']
})
export class ConfirmDialogComponent {
  /**
   * Dialog title
   */
  @Input() title: string = 'Confirm Action';

  /**
   * Confirmation message to display
   */
  @Input() message: string = 'Are you sure you want to proceed?';

  /**
   * Confirm button label
   */
  @Input() confirmLabel: string = 'Confirm';

  /**
   * Cancel button label
   */
  @Input() cancelLabel: string = 'Cancel';

  /**
   * Confirm button style variant
   */
  @Input() variant: 'danger' | 'primary' | 'warning' = 'danger';

  /**
   * Emitted when confirm button is clicked
   */
  @Output() confirm = new EventEmitter<void>();

  /**
   * Emitted when cancel button is clicked or backdrop is clicked
   */
  @Output() cancel = new EventEmitter<void>();

  /**
   * Handles confirm button click
   */
  onConfirm(): void {
    this.confirm.emit();
  }

  /**
   * Handles cancel button click
   */
  onCancel(): void {
    this.cancel.emit();
  }

  /**
   * Handles backdrop click (clicking outside dialog)
   */
  onBackdropClick(event: MouseEvent): void {
    // Only close if clicking the backdrop itself, not the dialog content
    if (event.target === event.currentTarget) {
      this.onCancel();
    }
  }

  /**
   * Handles keyboard events (ESC to cancel)
   */
  onKeyDown(event: KeyboardEvent): void {
    if (event.key === 'Escape') {
      this.onCancel();
    }
  }
}
