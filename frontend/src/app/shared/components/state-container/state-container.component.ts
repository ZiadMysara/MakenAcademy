import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';

/**
 * StateContainerComponent
 * 
 * Generic wrapper component that handles loading, error, empty, and data states.
 * Provides a consistent UI pattern for all feature components.
 * 
 * Constitution Compliance:
 * - [FEAT-025] Loading, error, and empty states handled explicitly
 * - [FE-008] Reusable base component from Frontend Core
 * - Mobile-first responsive design (Constitution §12.I)
 * 
 * Usage:
 * <app-state-container 
 *   [loading]="isLoading" 
 *   [error]="errorMessage" 
 *   [empty]="isEmpty"
 *   (retry)="onRetry()">
 *   <div>Your content here</div>
 * </app-state-container>
 */
@Component({
  selector: 'app-state-container',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './state-container.component.html',
  styleUrls: ['./state-container.component.css']
})
export class StateContainerComponent {
  /**
   * Indicates if content is currently loading
   */
  @Input() loading: boolean = false;

  /**
   * Error message to display (null if no error)
   */
  @Input() error: string | null = null;

  /**
   * Indicates if the data set is empty
   */
  @Input() empty: boolean = false;

  /**
   * Emitted when user clicks retry button (on error state)
   */
  @Output() retry = new EventEmitter<void>();

  /**
   * Handles retry button click
   */
  onRetry(): void {
    this.retry.emit();
  }

  /**
   * Determines which state to display
   * Priority: loading > error > empty > data
   */
  get currentState(): 'loading' | 'error' | 'empty' | 'data' {
    if (this.loading) return 'loading';
    if (this.error) return 'error';
    if (this.empty) return 'empty';
    return 'data';
  }
}
